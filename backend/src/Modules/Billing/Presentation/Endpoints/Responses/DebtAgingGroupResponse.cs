namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record DebtAgingGroupResponse(
    string GroupCode,
    string GroupName,
    int InvoiceCount,
    long TotalAmount);
