namespace HomeMatch.Modules.Billing.Application.Models;

public sealed record VNPayPaymentRequest(
    string PaymentCode,
    long Amount,
    string OrderInfo,
    string ClientIpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiredAt);
