namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record PaymentDto(
    long Id,
    string PaymentCode,
    long InvoiceId,
    long Amount,
    string PaymentMethod,
    string? GatewayTransactionNo,
    string? BankCode,
    string? CardType,
    string? GatewayResponseCode,
    string? PaymentUrl,
    string Status,
    DateTimeOffset ExpiredAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);
