using HomeMatch.Modules.Billing.Application.DTOs;

namespace HomeMatch.Modules.Billing.Application.Queries.GetInvoices;

public sealed record GetInvoicesQuery(
    InvoiceFilter Filter,
    int Page,
    int PageSize,
    long RequestUserId,
    bool IsLandlord,
    bool IsTenant,
    bool IsAdmin);
