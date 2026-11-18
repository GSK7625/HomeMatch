namespace HomeMatch.Modules.Billing.Infrastructure.Services;

public class VNPayOptions
{
    public const string SectionName = "VNPay";

    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnUrl { get; set; } = "http://localhost:3000/billing/payment-callback";
    public string Command { get; set; } = "pay";
    public string CurrCode { get; set; } = "VND";
    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";
    public int ExpirationMinutes { get; set; } = 15;
}
