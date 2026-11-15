namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record PagedInvoicesResponse(
    IReadOnlyList<InvoiceResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
