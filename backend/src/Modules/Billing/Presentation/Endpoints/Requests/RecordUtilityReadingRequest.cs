namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Requests;

public sealed record RecordUtilityReadingRequest(
    long ContractId,
    int BillingMonth,
    int BillingYear,
    int ElectricityOld,
    int ElectricityNew,
    int WaterOld,
    int WaterNew,
    long? ElectricityUnitPriceOverride = null,
    long? WaterUnitPriceOverride = null,
    long? ServiceCostOverride = null,
    DateOnly? ReadingDate = null);
