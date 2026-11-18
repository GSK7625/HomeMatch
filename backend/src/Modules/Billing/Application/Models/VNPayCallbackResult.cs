namespace HomeMatch.Modules.Billing.Application.Models;

public sealed record VNPayCallbackResult(
    bool IsValidSignature,
    bool IsSuccess,
    string PaymentCode,
    string? GatewayTransactionNo,
    string? BankCode,
    string? CardType,
    string ResponseCode,
    string? TransactionStatus,
    long AmountInVnd,
    string RawData,
    string? ErrorMessage = null);
