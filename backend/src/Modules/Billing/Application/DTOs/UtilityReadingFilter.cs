namespace HomeMatch.Modules.Billing.Application.DTOs;

public sealed record UtilityReadingFilter(
    long? PropertyId = null,
    long? RoomId = null,
    long? ContractId = null,
    long? LandlordId = null,
    long? TenantId = null,
    int? BillingMonth = null,
    int? BillingYear = null);
