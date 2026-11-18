namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractsByTenant;

public sealed record GetContractsByTenantQuery(
    long TenantId,
    string? Status = null,
    int Page = 1,
    int PageSize = 20);
