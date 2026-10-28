using HomeMatch.Modules.Properties.Domain.Entities;

namespace HomeMatch.Modules.Properties.Application;

public static class TariffSelection
{
    public static IReadOnlyList<UtilityTariff> EffectiveForProperty(
        IEnumerable<UtilityTariff> rates, DateOnly date) =>
        rates.Where(rate => rate.EffectiveFrom <= date && rate.RoomId is null)
            .GroupBy(rate => rate.TariffType)
            .Select(group => group.OrderByDescending(rate => rate.EffectiveFrom).First())
            .OrderBy(rate => rate.TariffType).ToArray();
}
