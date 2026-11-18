namespace HomeMatch.Modules.Billing.Application.Queries.GetDebtSummary;

public sealed record GetDebtSummaryQuery(
    long? PropertyId,
    long? ContractId,
    long UserId,
    bool IsLandlord,
    bool IsTenant,
    bool IsAdmin);
