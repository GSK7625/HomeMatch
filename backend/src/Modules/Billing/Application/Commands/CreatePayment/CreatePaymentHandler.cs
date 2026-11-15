using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Models;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Application.Commands.CreatePayment;

public class CreatePaymentHandler(
    IInvoiceRepository invoiceRepository,
    IPaymentRepository paymentRepository,
    IVNPayGateway vnpayGateway)
{
    public async Task<Result<PaymentDto>> HandleAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<PaymentDto>(BillingErrors.InvoiceNotFound);
        }

        // Chỉ Người thuê của hợp đồng hoặc Quản trị viên mới được thanh toán hóa đơn
        if (!command.IsAdmin && invoice.TenantId != command.UserId)
        {
            return Result.Failure<PaymentDto>(BillingErrors.Forbidden);
        }

        // Hóa đơn đã thanh toán hoặc đã hủy thì không được thanh toán
        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Result.Failure<PaymentDto>(BillingErrors.InvoiceAlreadyPaid);
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Result.Failure<PaymentDto>(BillingErrors.InvoiceCancelled);
        }

        // Kiểm tra giao dịch PENDING (BR-BIL-22: Mỗi hóa đơn chỉ có tối đa 1 giao dịch PENDING)
        var pendingPayment = await paymentRepository.GetPendingPaymentByInvoiceIdAsync(invoice.Id, cancellationToken);
        if (pendingPayment is not null)
        {
            if (DateTimeOffset.UtcNow >= pendingPayment.ExpiredAt)
            {
                // Giao dịch PENDING cũ đã hết hạn -> đánh dấu EXPIRED để cho phép thử lại
                pendingPayment.MarkExpired();
                await paymentRepository.UpdateAsync(pendingPayment, cancellationToken);
            }
            else
            {
                // Vẫn còn giao dịch PENDING hiệu lực
                return Result.Failure<PaymentDto>(BillingErrors.PendingPaymentAlreadyExists);
            }
        }

        // Sinh mã giao dịch duy nhất
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var randomSuffix = Random.Shared.Next(100, 999);
        var paymentCode = $"PAY{timestamp}{randomSuffix}";

        var paymentResult = Payment.CreatePending(
            paymentCode: paymentCode,
            invoiceId: invoice.Id,
            amount: invoice.TotalAmount,
            paymentMethod: PaymentMethod.VNPay,
            expirationMinutes: 15);

        if (paymentResult.IsFailure)
        {
            return Result.Failure<PaymentDto>(paymentResult.Error);
        }

        var payment = paymentResult.Value;

        // Sinh link thanh toán VNPay
        var vnpayRequest = new VNPayPaymentRequest(
            PaymentCode: payment.PaymentCode,
            Amount: payment.Amount,
            OrderInfo: $"Thanh toan hoa don {invoice.InvoiceCode}",
            ClientIpAddress: command.ClientIpAddress,
            CreatedAt: payment.CreatedAt,
            ExpiredAt: payment.ExpiredAt);

        var paymentUrl = vnpayGateway.CreatePaymentUrl(vnpayRequest);
        payment.PaymentUrl = paymentUrl;

        await paymentRepository.AddAsync(payment, cancellationToken);

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
