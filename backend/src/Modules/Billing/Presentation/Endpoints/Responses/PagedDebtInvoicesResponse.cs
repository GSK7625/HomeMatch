namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record PagedDebtInvoicesResponse(
    IReadOnlyList<DebtInvoiceResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
