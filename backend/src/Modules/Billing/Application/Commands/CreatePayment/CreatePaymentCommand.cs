namespace HomeMatch.Modules.Billing.Application.Commands.CreatePayment;

public sealed record CreatePaymentCommand(
    long InvoiceId,
    long UserId,
    bool IsAdmin,
    string ClientIpAddress);
