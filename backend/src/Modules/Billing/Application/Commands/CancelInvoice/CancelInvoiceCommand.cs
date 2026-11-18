namespace HomeMatch.Modules.Billing.Application.Commands.CancelInvoice;

public sealed record CancelInvoiceCommand(
    long InvoiceId,
    string Reason,
    long RequestUserId,
    bool IsAdmin);
