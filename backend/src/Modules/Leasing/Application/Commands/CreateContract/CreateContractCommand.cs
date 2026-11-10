namespace HomeMatch.Modules.Leasing.Application.Commands.CreateContract;

public sealed record CreateContractCommand(
    long LandlordId,
    long RoomId,
    long TenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    int BillingDueDay = 5,
    string? ContractCode = null,
    string? ContractFileUrl = null);
