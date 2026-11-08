using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Tariffs.GetPropertyTariffs;

public sealed class GetPropertyTariffsHandler
{
    private readonly IPropertyRepository _properties;
    private readonly ITariffRepository _tariffs;

    public GetPropertyTariffsHandler(IPropertyRepository properties, ITariffRepository tariffs)
    {
        _properties = properties;
        _tariffs = tariffs;
    }

    public async Task<Result<IReadOnlyList<TariffResult>>> HandleAsync(
        GetPropertyTariffsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(query.LandlordId, query.PropertyId, cancellationToken))
            return Result.Failure<IReadOnlyList<TariffResult>>(PropertiesErrors.PropertyNotFound);

        var rates = await _tariffs.GetPropertyTariffsAsync(query.PropertyId, cancellationToken);
        return Result.Success<IReadOnlyList<TariffResult>>(rates.Select(PropertyMapping.ToResult).ToArray());
    }
}

