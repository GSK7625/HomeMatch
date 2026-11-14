namespace HomeMatch.Modules.Leasing.Application.Queries.GetExpiringContracts;

public sealed record GetExpiringContractsQuery(
    long LandlordId,
    int Days = 30);
