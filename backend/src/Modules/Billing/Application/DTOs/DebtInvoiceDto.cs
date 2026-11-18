namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record DebtInvoiceDto(
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
