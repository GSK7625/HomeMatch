namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record PagedDebtInvoicesDto(
    IReadOnlyList<DebtInvoiceDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
