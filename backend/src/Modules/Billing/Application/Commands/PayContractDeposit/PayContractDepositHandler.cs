using HomeMatch.Modules.Billing.Application.Commands.CreatePayment;
using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Leasing;

namespace HomeMatch.Modules.Billing.Application.Commands.PayContractDeposit;

/// <summary>
/// Khởi tạo giao dịch thanh toán tiền cọc / kích hoạt hợp đồng ngay sau khi ký xong (SIGNED)
/// </summary>
public class PayContractDepositHandler(
    ILeasingContractReader leasingContractReader,
    IRoomListingReader roomListingReader,
    IInvoiceRepository invoiceRepository,
    IPaymentRepository paymentRepository,
    CreatePaymentHandler createPaymentHandler)
{
    public async Task<Result<PaymentDto>> HandleAsync(
        PayContractDepositCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await leasingContractReader.GetContractByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<PaymentDto>(new Error("Billing.ContractNotFound", "Không tìm thấy hợp đồng yêu cầu."));
        }

        if (!command.IsAdmin && contract.TenantId != command.UserId)
        {
            return Result.Failure<PaymentDto>(BillingErrors.Forbidden);
        }

        if (!string.Equals(contract.Status, "SIGNED", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PaymentDto>(new Error(
                "Billing.ContractNotSigned",
                "Hợp đồng phải được cả hai bên ký hoàn tất trước khi thanh toán kích hoạt."));
        }

        var invoiceCode = $"INV-DEP-{contract.ContractCode}";
        var invoice = await invoiceRepository.GetByCodeAsync(invoiceCode, cancellationToken);

        if (invoice is null)
        {
            var amount = contract.DepositAmount > 0 ? contract.DepositAmount : contract.MonthlyRent;
            var today = BusinessDate.Today();
            var roomSnapshot = await roomListingReader.GetRoomAsync(contract.RoomId, cancellationToken);
            var propertyId = roomSnapshot?.PropertyId ?? 0;

            var createResult = Invoice.Create(
                invoiceCode: invoiceCode,
                contractId: contract.Id,
                landlordId: contract.LandlordId,
                tenantId: contract.TenantId,
                roomId: contract.RoomId,
                propertyId: propertyId,
                billingMonth: today.Month,
                billingYear: today.Year,
                rentAmount: amount,
                dueDate: today.AddDays(3));

            if (createResult.IsFailure)
            {
                return Result.Failure<PaymentDto>(createResult.Error);
            }

            invoice = createResult.Value;
            await invoiceRepository.AddAsync(invoice, cancellationToken);
        }
        else if (invoice.Status == InvoiceStatus.Paid)
        {
            return Result.Failure<PaymentDto>(BillingErrors.InvoiceAlreadyPaid);
        }

        // Nếu hóa đơn đang có giao dịch PENDING còn hạn -> trả về luôn để người thuê tiếp tục thanh toán
        var pendingPayment = await paymentRepository.GetPendingPaymentByInvoiceIdAsync(invoice.Id, cancellationToken);
        if (pendingPayment is not null && DateTimeOffset.UtcNow < pendingPayment.ExpiredAt && !string.IsNullOrWhiteSpace(pendingPayment.PaymentUrl))
        {
            return Result.Success(new PaymentDto(
                pendingPayment.Id,
                pendingPayment.PaymentCode,
                pendingPayment.InvoiceId,
                pendingPayment.Amount,
                pendingPayment.PaymentMethod,
                pendingPayment.GatewayTransactionNo,
                pendingPayment.BankCode,
                pendingPayment.CardType,
                pendingPayment.GatewayResponseCode,
                pendingPayment.PaymentUrl,
                pendingPayment.Status,
                pendingPayment.ExpiredAt,
                pendingPayment.CompletedAt,
                pendingPayment.CreatedAt));
        }

        return await createPaymentHandler.HandleAsync(
            new CreatePaymentCommand(invoice.Id, command.UserId, command.IsAdmin, command.ClientIpAddress),
            cancellationToken);
    }
}
