using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record TariffResponse(
    long Id,
    long PropertyId,
    long? RoomId,
    string TariffType,
    string CalcMethod,
    long UnitPrice,
    DateOnly EffectiveFrom)
{
    public static TariffResponse From(TariffResult value) =>
        new(
            value.Id,
            value.PropertyId,
            value.RoomId,
            value.TariffType,
            value.CalcMethod,
            value.UnitPrice,
            value.EffectiveFrom);
}
