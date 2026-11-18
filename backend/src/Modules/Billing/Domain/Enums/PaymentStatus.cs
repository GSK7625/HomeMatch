namespace HomeMatch.Modules.Billing.Domain.Enums;

/// <summary>
/// Trạng thái giao dịch thanh toán theo đặc tả Mục 5.2 BRD
/// </summary>
public static class PaymentStatus
{
    /// <summary>
    /// Giao dịch đã được tạo và đang chờ kết quả từ VNPay
    /// </summary>
    public const string Pending = "PENDING";

    /// <summary>
    /// Giao dịch thanh toán thành công được xác thực bởi backend
    /// </summary>
    public const string Success = "SUCCESS";

    /// <summary>
    /// Giao dịch thanh toán thất bại
    /// </summary>
    public const string Failed = "FAILED";

    /// <summary>
    /// Giao dịch đã hết thời hạn thanh toán (15 phút)
    /// </summary>
    public const string Expired = "EXPIRED";

    public static readonly string[] All = [Pending, Success, Failed, Expired];

    public static bool IsValid(string status) => All.Contains(status);
}
