using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Rooms.GetPropertyRooms;

public sealed class GetPropertyRoomsHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomRepository _rooms;

    public GetPropertyRoomsHandler(IPropertyRepository properties, IRoomRepository rooms)
    {
        _properties = properties;
        _rooms = rooms;
    }

    public async Task<Result<IReadOnlyList<RoomResult>>> HandleAsync(GetPropertyRoomsQuery command, CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure<IReadOnlyList<RoomResult>>(PropertiesErrors.PropertyNotFound);
        return Result.Success(await _rooms.GetByPropertyAsync(command.PropertyId, cancellationToken));
    }
}
