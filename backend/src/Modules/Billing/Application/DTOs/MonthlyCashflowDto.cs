namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record MonthlyCashflowDto(
    int Year,
    int Month,
    long BilledAmount,
    long CollectedAmount,
    long OutstandingAmount);
