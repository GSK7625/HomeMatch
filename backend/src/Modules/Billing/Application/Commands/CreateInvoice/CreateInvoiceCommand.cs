namespace HomeMatch.Modules.Billing.Application.Commands.CreateInvoice;

public sealed record CreateInvoiceCommand(
    long ContractId,
    int BillingMonth,
    int BillingYear,
    long RequestUserId,
    bool IsAdmin);
