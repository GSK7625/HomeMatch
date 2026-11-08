using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface ITariffRepository
{
    Task<IReadOnlyList<UtilityTariff>> GetPropertyTariffsAsync(long propertyId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<UtilityTariff>>> SetPropertyTariffsAsync(
        long propertyId,
        IReadOnlyList<(string TariffType, string CalcMethod, long UnitPrice)> tariffs,
        CancellationToken cancellationToken);
}
