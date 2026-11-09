using HomeMatch.Modules.Properties.Domain.Entities;

namespace HomeMatch.Modules.Properties.Application;

public static class TariffSelection
{
    public static IReadOnlyList<UtilityTariff> EffectiveForProperty(
        IEnumerable<UtilityTariff> rates) =>
        rates.Where(rate => rate.RoomId is null)
            .OrderBy(rate => rate.TariffType).ToArray();
}
