namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public sealed record DebtInvoiceResponse(
    long Id,
    string InvoiceCode,
    long ContractId,
    long LandlordId,
    long TenantId,
    long RoomId,
    long PropertyId,
    int BillingMonth,
    int BillingYear,
    long TotalAmount,
    DateOnly DueDate,
    int DaysOverdue,
    string AgingGroup,
    string Status,
    DateTimeOffset CreatedAt);
