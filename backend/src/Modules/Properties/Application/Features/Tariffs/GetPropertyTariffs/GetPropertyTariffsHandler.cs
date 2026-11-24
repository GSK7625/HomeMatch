using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Tariffs.GetPropertyTariffs;

public sealed class GetPropertyTariffsHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomRepository _rooms;
    private readonly ITariffRepository _tariffs;

    public GetPropertyTariffsHandler(IPropertyRepository properties, IRoomRepository rooms, ITariffRepository tariffs)
    {
        _properties = properties;
        _rooms = rooms;
        _tariffs = tariffs;
    }

    public async Task<Result<IReadOnlyList<TariffResult>>> HandleAsync(
        GetPropertyTariffsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(query.LandlordId, query.PropertyId, cancellationToken))
            return Result.Failure<IReadOnlyList<TariffResult>>(PropertiesErrors.PropertyNotFound);

        if (query.RoomId.HasValue)
        {
            var room = await _rooms.GetAsync(query.PropertyId, query.RoomId.Value, cancellationToken);
            if (room is null)
                return Result.Failure<IReadOnlyList<TariffResult>>(PropertiesErrors.RoomNotFound);
        }

        var rates = await _tariffs.GetPropertyTariffsAsync(query.PropertyId, cancellationToken, query.RoomId);
        return Result.Success<IReadOnlyList<TariffResult>>(rates.Select(PropertyMapping.ToResult).ToArray());
    }
}

