namespace HomeMatch.Modules.Billing.Application.Queries.GetMonthlyCashflow;

public sealed record GetMonthlyCashflowQuery(
    long? PropertyId,
    int? Year,
    long UserId,
    bool IsLandlord,
    bool IsAdmin);
