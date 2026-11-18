namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record InvoiceFilter(
    long? ContractId = null,
    long? PropertyId = null,
    long? LandlordId = null,
    long? TenantId = null,
    string? Status = null,
    int? BillingMonth = null,
    int? BillingYear = null);
