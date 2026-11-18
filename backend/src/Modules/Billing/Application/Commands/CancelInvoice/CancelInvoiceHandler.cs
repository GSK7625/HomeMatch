using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Application.Commands.CancelInvoice;

public class CancelInvoiceHandler
{
    private readonly IInvoiceRepository _invoiceRepository;

    public CancelInvoiceHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceDto>> HandleAsync(CancelInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure<InvoiceDto>(new Error("Billing.ReasonRequired", "Vui lòng cung cấp lý do hủy hóa đơn."));
        }

        var invoice = await _invoiceRepository.GetByIdWithPaymentsAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>(BillingErrors.InvoiceNotFound);
        }

        // Kiểm tra phân quyền: Chỉ chủ nhà sở hữu hóa đơn hoặc Admin mới được hủy (BR-BIL-14)
        if (!command.IsAdmin && invoice.LandlordId != command.RequestUserId)
        {
            return Result.Failure<InvoiceDto>(BillingErrors.Forbidden);
        }

        var cancelResult = invoice.Cancel(command.Reason);
        if (cancelResult.IsFailure)
        {
            return Result.Failure<InvoiceDto>(cancelResult.Error);
        }

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

        var dto = new InvoiceDto(
            invoice.Id,
            invoice.InvoiceCode,
            invoice.ContractId,
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
            invoice.UpdatedAt);

        return Result.Success(dto);
    }
}
