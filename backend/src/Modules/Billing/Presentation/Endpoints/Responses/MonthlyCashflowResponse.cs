namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record MonthlyCashflowResponse(
    int Year,
    int Month,
    long BilledAmount,
    long CollectedAmount,
    long OutstandingAmount);
