using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Rooms.GetRoomById;

public sealed class GetRoomByIdHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomRepository _rooms;

    public GetRoomByIdHandler(IPropertyRepository properties, IRoomRepository rooms)
    {
        _properties = properties;
        _rooms = rooms;
    }

    public async Task<Result<RoomResult>> HandleAsync(GetRoomByIdQuery command, CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.PropertyNotFound);
        var room = await _rooms.GetAsync(command.PropertyId, command.RoomId, cancellationToken);
        if (room is null) return Result.Failure<RoomResult>(PropertiesErrors.RoomNotFound);
        var amenityIds = await _rooms.GetAmenityIdsAsync(room.Id, cancellationToken);
        return Result.Success(PropertyMapping.ToResult(room, amenityIds));
    }
}
