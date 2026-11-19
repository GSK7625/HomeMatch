namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record FinancialSummaryReportDto(
    long TotalReceivable,
    long TotalCollected,
    long TotalOutstanding,
    long TotalDepositHolding,
    int TotalInvoicesCount,
    int PaidInvoicesCount,
    int UnpaidInvoicesCount,
    int OverdueInvoicesCount,
    double CollectionRate);
