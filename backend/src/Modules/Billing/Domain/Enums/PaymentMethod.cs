namespace HomeMatch.Modules.Billing.Domain.Enums;

/// <summary>
/// Phương thức thanh toán hóa đơn
/// </summary>
public static class PaymentMethod
{
    public const string VNPay = "VNPAY";
    public const string BankTransfer = "BANK_TRANSFER";
    public const string Cash = "CASH";
    public const string MoMo = "MOMO";

    public static readonly string[] All = [VNPay, BankTransfer, Cash, MoMo];

    public static bool IsValid(string method) => All.Contains(method);
}
