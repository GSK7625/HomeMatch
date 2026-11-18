namespace HomeMatch.Modules.Billing.Domain.Enums;

/// <summary>
/// Trạng thái hóa đơn tiền phòng theo đặc tả Mục 5.1 BRD
/// </summary>
public static class InvoiceStatus
{
    /// <summary>
    /// Hóa đơn chưa được thanh toán và chưa quá hạn
    /// </summary>
    public const string Unpaid = "UNPAID";

    /// <summary>
    /// Hóa đơn chưa thanh toán và đã quá hạn
    /// </summary>
    public const string Overdue = "OVERDUE";

    /// <summary>
    /// Hóa đơn đã thanh toán thành công
    /// </summary>
    public const string Paid = "PAID";

    /// <summary>
    /// Hóa đơn đã bị hủy bởi chủ nhà
    /// </summary>
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All = [Unpaid, Overdue, Paid, Cancelled];

    public static bool IsValid(string status) => All.Contains(status);
}
