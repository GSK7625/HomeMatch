namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record PropertyFinancialResponse(
    long PropertyId,
    long TotalReceivable,
    long TotalCollected,
    long TotalOutstanding,
    int InvoicesCount,
    double CollectionRate);
