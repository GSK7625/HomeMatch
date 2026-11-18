namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record FinancialSummaryResponse(
    long TotalReceivable,
    long TotalCollected,
    long TotalOutstanding,
    long TotalDepositHolding,
    int TotalInvoicesCount,
    int PaidInvoicesCount,
    int UnpaidInvoicesCount,
    int OverdueInvoicesCount,
    double CollectionRate);
