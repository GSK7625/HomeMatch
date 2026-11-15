namespace HomeMatch.Modules.Billing.Application.Queries.GetFinancialSummary;

public sealed record GetFinancialSummaryQuery(
    long? PropertyId,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    long UserId,
    bool IsLandlord,
    bool IsAdmin);
