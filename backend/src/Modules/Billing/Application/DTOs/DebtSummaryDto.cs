namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record DebtSummaryDto(
    long TotalUnpaidDebt,
    long TotalOverdueDebt,
    int TotalUnpaidInvoices,
    int TotalOverdueInvoices,
    IReadOnlyList<DebtAgingGroupDto> AgingGroups);
