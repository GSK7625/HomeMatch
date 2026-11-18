namespace HomeMatch.Modules.Billing.Application.Models;

public sealed record VNPayIpnResponse(string RspCode, string Message)
{
    public static VNPayIpnResponse Success() => new("00", "Confirm Success");
    public static VNPayIpnResponse OrderNotFound() => new("01", "Order not found");
    public static VNPayIpnResponse OrderAlreadyConfirmed() => new("02", "Order already confirmed");
    public static VNPayIpnResponse InvalidAmount() => new("04", "Invalid amount");
    public static VNPayIpnResponse InvalidSignature() => new("97", "Invalid checksum");
    public static VNPayIpnResponse UnknownError(string message = "Unknown error") => new("99", message);
}
