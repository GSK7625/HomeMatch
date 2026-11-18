namespace HomeMatch.Modules.Billing.Application.Queries.GetPropertyFinancialReport;

public sealed record GetPropertyFinancialReportQuery(
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    long UserId,
    bool IsLandlord,
    bool IsAdmin);
