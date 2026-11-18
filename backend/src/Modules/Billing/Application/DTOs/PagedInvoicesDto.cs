namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record PagedInvoicesDto(
    IReadOnlyList<InvoiceDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
