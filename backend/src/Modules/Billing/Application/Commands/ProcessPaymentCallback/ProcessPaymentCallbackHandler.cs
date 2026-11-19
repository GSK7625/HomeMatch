using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Models;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Leasing;
using HomeMatch.Shared.Contracts.Notifications;

namespace HomeMatch.Modules.Billing.Application.Commands.ProcessPaymentCallback;

public class ProcessPaymentCallbackHandler(
    IInvoiceRepository invoiceRepository,
    IPaymentRepository paymentRepository,
    IVNPayGateway vnpayGateway,
    ILeasingContractReader leasingContractReader,
    INotificationPublisher notificationPublisher)
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
            long? contractId = invoice?.ContractId;
            bool contractActivated = false;

            if (invoice is not null)
            {
                invoice.MarkAsPaid(completedAt);
                await invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // Tự động chuyển hợp đồng từ SIGNED -> ACTIVE khi người dùng hoàn tất thanh toán
                var existingContract = await leasingContractReader.GetContractByIdAsync(invoice.ContractId, cancellationToken);
                var wasSignedOnly = existingContract is not null &&
                                    string.Equals(existingContract.Status, "SIGNED", StringComparison.OrdinalIgnoreCase);

                contractActivated = await leasingContractReader.ActivateContractOnPaymentAsync(
                    invoice.ContractId,
                    payment.Amount,
                    invoice.TenantId,
                    cancellationToken);

                if (wasSignedOnly && contractActivated && existingContract is not null)
                {
                    await notificationPublisher.PublishAsync(
                        invoice.TenantId,
                        "Hợp đồng đã kích hoạt (ACTIVE)",
                        $"Thanh toán {payment.Amount:N0}đ thành công. Hợp đồng #{existingContract.ContractCode} đã chính thức chuyển sang trạng thái ACTIVE.",
                        "CONTRACT_ACTIVE",
                        "CONTRACT",
                        invoice.ContractId,
                        cancellationToken);

                    await notificationPublisher.PublishAsync(
                        invoice.LandlordId,
                        "Người thuê đã thanh toán & kích hoạt hợp đồng",
                        $"Người thuê đã thanh toán {payment.Amount:N0}đ cho hợp đồng #{existingContract.ContractCode}. Hợp đồng đã tự động chuyển sang trạng thái ACTIVE.",
                        "CONTRACT_ACTIVE",
                        "CONTRACT",
                        invoice.ContractId,
                        cancellationToken);
                }
            }

            await paymentRepository.UpdateAsync(payment, cancellationToken);

            return Result.Success(new ProcessPaymentCallbackResult(
                IsSuccess: true,
                IpnResponse: VNPayIpnResponse.Success(),
                Payment: MapToDto(payment),
                ErrorMessage: null,
                ContractId: contractId,
                ContractActivated: contractActivated));
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
