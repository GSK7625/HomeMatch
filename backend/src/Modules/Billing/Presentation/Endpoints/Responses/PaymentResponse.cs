namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record PaymentResponse(
    long Id,
    string PaymentCode,
    long InvoiceId,
    long Amount,
    string PaymentMethod,
    string? GatewayTransactionNo,
    string? BankCode,
    string? CardType,
    string? GatewayResponseCode,
    string Status,
    DateTimeOffset ExpiredAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);
