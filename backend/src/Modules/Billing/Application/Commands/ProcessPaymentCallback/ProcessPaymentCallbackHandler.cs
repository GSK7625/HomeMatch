using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Models;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Application.Commands.ProcessPaymentCallback;

public class ProcessPaymentCallbackHandler(
    IInvoiceRepository invoiceRepository,
    IPaymentRepository paymentRepository,
    IVNPayGateway vnpayGateway)
{
    public async Task<Result<ProcessPaymentCallbackResult>> HandleAsync(
        ProcessPaymentCallbackCommand command,
        CancellationToken cancellationToken = default)
    {
        var callback = vnpayGateway.ProcessCallback(command.QueryParameters);

        // 1. Kiểm tra chữ ký bảo mật (BR-BIL-24)
        if (!callback.IsValidSignature)
        {
            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: false,
                IpnResponse: VNPayIpnResponse.InvalidSignature(),
                Payment: null,
                ErrorMessage: callback.ErrorMessage ?? "Chữ ký bảo mật VNPay không hợp lệ."));
        }

        // 2. Tra cứu giao dịch trong hệ thống
        if (string.IsNullOrWhiteSpace(callback.PaymentCode))
        {
            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: false,
                IpnResponse: VNPayIpnResponse.OrderNotFound(),
                Payment: null,
                ErrorMessage: "Thiếu mã giao dịch thanh toán trong phản hồi."));
        }

        var payment = await paymentRepository.GetByCodeAsync(callback.PaymentCode, cancellationToken);
        if (payment is null)
        {
            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: false,
                IpnResponse: VNPayIpnResponse.OrderNotFound(),
                Payment: null,
                ErrorMessage: $"Không tìm thấy giao dịch {callback.PaymentCode}."));
        }

        // 3. Kiểm tra số tiền thanh toán (BR-BIL-21: chỉ thanh toán toàn bộ)
        if (callback.AmountInVnd > 0 && callback.AmountInVnd != payment.Amount)
        {
            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: false,
                IpnResponse: VNPayIpnResponse.InvalidAmount(),
                Payment: MapToDto(payment),
                ErrorMessage: $"Số tiền không khớp (Hóa đơn: {payment.Amount}, VNPay: {callback.AmountInVnd})."));
        }

        // 4. Kiểm tra Idempotency (BR-BIL-25: Không ghi nhận trùng khi callback gửi nhiều lần)
        if (payment.Status == PaymentStatus.Success)
        {
            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: true,
                IpnResponse: VNPayIpnResponse.OrderAlreadyConfirmed(),
                Payment: MapToDto(payment),
                ErrorMessage: null));
        }

        if (payment.Status == PaymentStatus.Failed || payment.Status == PaymentStatus.Expired)
        {
            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: false,
                IpnResponse: VNPayIpnResponse.OrderAlreadyConfirmed(),
                Payment: MapToDto(payment),
                ErrorMessage: "Giao dịch đã kết thúc trước đó."));
        }

        // 5. Cập nhật trạng thái giao dịch
        var completedAt = DateTimeOffset.UtcNow;
        if (callback.IsSuccess)
        {
            payment.MarkSuccess(
                gatewayTransactionNo: callback.GatewayTransactionNo ?? string.Empty,
                bankCode: callback.BankCode,
                cardType: callback.CardType,
                gatewayResponseCode: callback.ResponseCode,
                rawIpnData: callback.RawData,
                completedAt: completedAt);

            // Cập nhật hóa đơn sang PAID (BR-BIL-24)
            var invoice = await invoiceRepository.GetByIdAsync(payment.InvoiceId, cancellationToken);
            if (invoice is not null)
            {
                invoice.MarkAsPaid(completedAt);
                await invoiceRepository.UpdateAsync(invoice, cancellationToken);
            }

            await paymentRepository.UpdateAsync(payment, cancellationToken);

            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: true,
                IpnResponse: VNPayIpnResponse.Success(),
                Payment: MapToDto(payment),
                ErrorMessage: null));
        }
        else
        {
            payment.MarkFailed(
                gatewayResponseCode: callback.ResponseCode,
                rawIpnData: callback.RawData);

            await paymentRepository.UpdateAsync(payment, cancellationToken);

            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: false,
                IpnResponse: VNPayIpnResponse.Success(),
                Payment: MapToDto(payment),
                ErrorMessage: $"Giao dịch VNPay thất bại với mã phản hồi: {callback.ResponseCode}"));
        }
    }

    private static PaymentDto MapToDto(Domain.Entities.Payment payment) =>
        new(
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
}
