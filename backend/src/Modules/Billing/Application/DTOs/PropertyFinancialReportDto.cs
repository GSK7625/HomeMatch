namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record PropertyFinancialReportDto(
    long PropertyId,
    long TotalReceivable,
    long TotalCollected,
    long TotalOutstanding,
    int InvoicesCount,
    double CollectionRate);
