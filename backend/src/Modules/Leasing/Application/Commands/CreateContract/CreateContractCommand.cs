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
    Stream? FileStream = null,
    string? FileName = null,
    string? ContentType = null,
    long FileLength = 0,
    string? DocumentSnapshot = null,
    DateTimeOffset? SignatureExpiresAt = null,
    DateTimeOffset? HandoverDueAt = null);
