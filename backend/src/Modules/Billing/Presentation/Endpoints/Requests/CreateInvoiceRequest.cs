namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Requests;

public sealed record CreateInvoiceRequest(
    long ContractId,
    int BillingMonth,
    int BillingYear);
