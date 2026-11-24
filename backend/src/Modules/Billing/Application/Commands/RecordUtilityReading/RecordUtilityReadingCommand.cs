namespace HomeMatch.Modules.Billing.Application.Commands.RecordUtilityReading;

public sealed record RecordUtilityReadingCommand(
    long ContractId,
    int BillingMonth,
    int BillingYear,
    int ElectricityOld,
    int ElectricityNew,
    int WaterOld,
    int WaterNew,
    long? ElectricityUnitPriceOverride,
    long? WaterUnitPriceOverride,
    long? ServiceCostOverride,
    DateOnly? ReadingDate,
    long RequestUserId,
    bool IsAdmin);
