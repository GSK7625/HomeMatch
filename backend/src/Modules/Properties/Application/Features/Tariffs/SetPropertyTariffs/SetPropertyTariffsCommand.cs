namespace HomeMatch.Modules.Properties.Application.Features.Tariffs.SetPropertyTariffs;

public sealed record SetPropertyTariffsCommand(
    long LandlordId,
    long PropertyId,
    IReadOnlyList<TariffItemCommand> Tariffs,
    long? RoomId = null);

public sealed record TariffItemCommand(
    string TariffType,
    string CalcMethod,
    long UnitPrice);


