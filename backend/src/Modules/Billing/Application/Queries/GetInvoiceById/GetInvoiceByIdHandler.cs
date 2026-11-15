using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Leasing;

namespace HomeMatch.Modules.Billing.Application.Queries.GetInvoiceById;

public class GetInvoiceByIdHandler
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeasingContractReader _leasingContractReader;

    public GetInvoiceByIdHandler(
        IInvoiceRepository invoiceRepository,
        ILeasingContractReader leasingContractReader)
    {
        _invoiceRepository = invoiceRepository;
        _leasingContractReader = leasingContractReader;
    }

    public async Task<Result<InvoiceDetailDto>> HandleAsync(GetInvoiceByIdQuery query, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdWithPaymentsAsync(query.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDetailDto>(BillingErrors.InvoiceNotFound);
        }

        // Kiểm tra phân quyền: Chủ nhà sở hữu, Người thuê sở hữu hoặc Admin mới được xem
        bool isAuthorized = query.IsAdmin || invoice.LandlordId == query.RequestUserId || invoice.TenantId == query.RequestUserId;
        if (!isAuthorized)
        {
            return Result.Failure<InvoiceDetailDto>(BillingErrors.Forbidden);
        }

        var contract = await _leasingContractReader.GetContractByIdAsync(invoice.ContractId, cancellationToken);

        var paymentDtos = invoice.Payments
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id,
                p.PaymentCode,
                p.InvoiceId,
                p.Amount,
                p.PaymentMethod,
                p.GatewayTransactionNo,
                p.BankCode,
                p.CardType,
                p.GatewayResponseCode,
                p.Status,
                p.ExpiredAt,
                p.CompletedAt,
                p.CreatedAt))
            .ToList();

        var detailDto = new InvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceCode,
            invoice.ContractId,
            contract?.ContractCode,
            invoice.LandlordId,
            invoice.TenantId,
            invoice.RoomId,
            invoice.PropertyId,
            invoice.BillingMonth,
            invoice.BillingYear,
            invoice.RentAmount,
            invoice.ElectricityAmount,
            invoice.WaterAmount,
            invoice.ServiceAmount,
            invoice.DiscountAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.Status,
            invoice.CancelReason,
            invoice.CancelledAt,
            invoice.CreatedAt,
            invoice.UpdatedAt,
            paymentDtos);

        return Result.Success(detailDto);
    }
}
