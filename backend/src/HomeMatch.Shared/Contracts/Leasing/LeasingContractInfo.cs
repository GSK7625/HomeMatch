namespace HomeMatch.Shared.Contracts.Leasing;

public sealed record LeasingContractInfo(
    long Id,
    string ContractCode,
    long RoomId,
    long TenantId,
    long LandlordId,
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    int BillingDueDay,
    string Status);
