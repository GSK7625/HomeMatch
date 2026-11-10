namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractsByLandlord;

public sealed record GetContractsByLandlordQuery(
    long LandlordId,
    string? Status = null,
    int Page = 1,
    int PageSize = 20);
