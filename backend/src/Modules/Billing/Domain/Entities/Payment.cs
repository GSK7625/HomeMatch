using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Domain.Entities;

/// <summary>
/// Thực thể Giao dịch thanh toán hóa đơn
/// </summary>
public class Payment
{
    public long Id { get; set; }

    /// <summary>
    /// Mã giao dịch thanh toán nội bộ (vnp_TxnRef)
    /// </summary>
    public string PaymentCode { get; set; } = string.Empty;

    public long InvoiceId { get; set; }
    public long Amount { get; set; }
    public string PaymentMethod { get; set; } = Enums.PaymentMethod.VNPay;

    // Thông tin cổng thanh toán VNPay
    public string? GatewayTransactionNo { get; set; }
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    public string? GatewayResponseCode { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ProofImageUrl { get; set; } // Hỗ trợ chuyển khoản thủ công nếu có

    // Trạng thái giao dịch (Mục 5.2 BRD)
    public string Status { get; set; } = PaymentStatus.Pending;

    // Hạn thanh toán & Thời điểm hoàn tất (BR-BIL-22, BR-BIL-28)
    public DateTimeOffset ExpiredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? RawIpnData { get; set; }

    public long? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property nội bộ module
    public Invoice? Invoice { get; set; }

    /// <summary>
    /// Factory tạo giao dịch thanh toán mới ở trạng thái PENDING
    /// </summary>
    public static Result<Payment> CreatePending(
        string paymentCode,
        long invoiceId,
        long amount,
        string paymentMethod = Enums.PaymentMethod.VNPay,
        int expirationMinutes = 15)
    {
        if (string.IsNullOrWhiteSpace(paymentCode))
        {
            return Result.Failure<Payment>(new Error("Payment.InvalidCode", "Mã giao dịch thanh toán không được để trống."));
        }

        if (amount <= 0)
        {
            return Result.Failure<Payment>(new Error("Payment.InvalidAmount", "Số tiền thanh toán phải lớn hơn 0."));
        }

        var payment = new Payment
        {
            PaymentCode = paymentCode,
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending,
            ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(payment);
    }

    /// <summary>
    /// Đánh dấu giao dịch thanh toán thành công
    /// </summary>
    public void MarkSuccess(
        string gatewayTransactionNo,
        string? bankCode,
        string? cardType,
        string gatewayResponseCode,
        string? rawIpnData,
        DateTimeOffset completedAt)
    {
        GatewayTransactionNo = gatewayTransactionNo;
        BankCode = bankCode;
        CardType = cardType;
        GatewayResponseCode = gatewayResponseCode;
        RawIpnData = rawIpnData;
        CompletedAt = completedAt;
        Status = PaymentStatus.Success;
    }

    /// <summary>
    /// Đánh dấu giao dịch thanh toán thất bại
    /// </summary>
    public void MarkFailed(string gatewayResponseCode, string? rawIpnData)
    {
        GatewayResponseCode = gatewayResponseCode;
        RawIpnData = rawIpnData;
        Status = PaymentStatus.Failed;
    }

    /// <summary>
    /// Đánh dấu giao dịch hết hạn thanh toán
    /// </summary>
    public void MarkExpired()
    {
        if (Status == PaymentStatus.Pending)
        {
            Status = PaymentStatus.Expired;
        }
    }
}
