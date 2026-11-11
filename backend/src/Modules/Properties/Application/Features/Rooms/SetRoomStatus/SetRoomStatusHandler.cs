using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Properties.Application.Features.Rooms.SetRoomStatus;

public sealed class SetRoomStatusHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomRepository _rooms;
    private readonly IListingVisibilityWriter _listingVisibility;

    public SetRoomStatusHandler(IPropertyRepository properties, IRoomRepository rooms, IListingVisibilityWriter listingVisibility)
    {
        _properties = properties;
        _rooms = rooms;
        _listingVisibility = listingVisibility;
    }

    public async Task<Result<RoomResult>> HandleAsync(SetRoomStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Status is not ("AVAILABLE" or "RENTED" or "SUSPENDED"))
            return Result.Failure<RoomResult>(PropertiesErrors.InvalidRoomStatus);
        if (!await _properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.PropertyNotFound);
        var room = await _rooms.GetAsync(command.PropertyId, command.RoomId, cancellationToken);
        if (room is null) return Result.Failure<RoomResult>(PropertiesErrors.RoomNotFound);
        room.Status = command.Status;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await _rooms.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<RoomResult>(saved.Error);
        if (command.Status != "AVAILABLE")
            await _listingVisibility.HidePublishedForRoomAsync(room.Id, cancellationToken);
        var amenityIds = await _rooms.GetAmenityIdsAsync(room.Id, cancellationToken);
        return Result.Success(PropertyMapping.ToResult(room, amenityIds));
    }
}
