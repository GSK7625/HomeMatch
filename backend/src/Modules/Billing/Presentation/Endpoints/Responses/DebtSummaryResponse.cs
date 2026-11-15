namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record DebtSummaryResponse(
    long TotalUnpaidDebt,
    long TotalOverdueDebt,
    int TotalUnpaidInvoices,
    int TotalOverdueInvoices,
    IReadOnlyList<DebtAgingGroupResponse> AgingGroups);
