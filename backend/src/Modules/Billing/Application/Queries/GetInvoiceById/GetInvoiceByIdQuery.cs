namespace HomeMatch.Modules.Billing.Application.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(
    long InvoiceId,
    long RequestUserId,
    bool IsAdmin);
