namespace HomeMatch.Modules.Billing.Application.Queries.GetDebtInvoices;

public sealed record GetDebtInvoicesQuery(
    long? PropertyId,
    long? ContractId,
    string? AgingGroup,
    int Page,
    int PageSize,
    long UserId,
    bool IsLandlord,
    bool IsTenant,
    bool IsAdmin);
