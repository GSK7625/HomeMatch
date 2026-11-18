using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Application.Queries.GetPaymentById;

public class GetPaymentByIdHandler(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository)
{
    public async Task<Result<PaymentDto>> HandleAsync(GetPaymentByIdQuery query, CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(query.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<PaymentDto>(BillingErrors.PaymentNotFound);
        }

        var invoice = await invoiceRepository.GetByIdAsync(payment.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<PaymentDto>(BillingErrors.InvoiceNotFound);
        }

        // Quyền truy cập: Chủ nhà (Landlord), Người thuê (Tenant) của hợp đồng, hoặc Quản trị viên (Admin)
        if (!query.IsAdmin && invoice.LandlordId != query.UserId && invoice.TenantId != query.UserId)
        {
            return Result.Failure<PaymentDto>(BillingErrors.Forbidden);
        }

        var dto = new PaymentDto(
            payment.Id,
            payment.PaymentCode,
            payment.InvoiceId,
            payment.Amount,
            payment.PaymentMethod,
            payment.GatewayTransactionNo,
            payment.BankCode,
            payment.CardType,
            payment.GatewayResponseCode,
            payment.PaymentUrl,
            payment.Status,
            payment.ExpiredAt,
            payment.CompletedAt,
            payment.CreatedAt);

        return Result.Success(dto);
    }
}
