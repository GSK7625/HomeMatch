using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Properties.Application.Features.Rooms.UpdateRoom;

public sealed class UpdateRoomHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomRepository _rooms;
    private readonly IRoomListingEditReader _listingEditReader;

    public UpdateRoomHandler(IPropertyRepository properties, IRoomRepository rooms, IRoomListingEditReader listingEditReader)
    {
        _properties = properties;
        _rooms = rooms;
        _listingEditReader = listingEditReader;
    }

    public async Task<Result<RoomResult>> HandleAsync(UpdateRoomCommand command, CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.PropertyNotFound);
        var room = await _rooms.GetAsync(command.PropertyId, command.RoomId, cancellationToken);
        if (room is null) return Result.Failure<RoomResult>(PropertiesErrors.RoomNotFound);
        // shortcut: this read does not serialize edits with submit/approve; use a shared lock if concurrent transitions must be atomic.
        if (await _listingEditReader.IsRoomEditBlockedAsync(room.Id, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.RoomEditLocked);
        var number = (command.RoomNumber ?? room.RoomNumber).Trim();
        var floor = command.Floor ?? room.Floor;
        var area = command.AreaSqm ?? room.AreaSqm;
        var type = command.RoomType ?? room.RoomType;
        var occupants = command.MaxOccupants ?? room.MaxOccupants;
        var error = RoomValidation.Validate(number, floor, area, type, occupants);
        if (error is not null) return Result.Failure<RoomResult>(error);
        if (number != room.RoomNumber && await _rooms.NumberExistsAsync(command.PropertyId, number, room.Id, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.DuplicateRoomNumber);
        var amenityIds = command.AmenityIds;
        var amenitiesChanged = false;
        if (amenityIds is not null)
        {
            if (amenityIds.Any(id => id <= 0) || amenityIds.Distinct().Count() != amenityIds.Count
                || !await _rooms.AmenitiesExistAsync(amenityIds, cancellationToken))
                return Result.Failure<RoomResult>(PropertiesErrors.InvalidAmenities);

            var currentAmenityIds = await _rooms.GetAmenityIdsAsync(room.Id, cancellationToken);
            amenitiesChanged = !currentAmenityIds.OrderBy(id => id).SequenceEqual(amenityIds.OrderBy(id => id));
        }

        if (amenityIds is not null && amenitiesChanged)
        {
            await _rooms.ReplaceAmenitiesAsync(room.Id, amenityIds, cancellationToken);
        }

        room.RoomNumber = number;
        room.Floor = floor;
        room.AreaSqm = area;
        room.RoomType = type;
        room.MaxOccupants = occupants;
        room.AvailableFrom = command.AvailableFrom ?? room.AvailableFrom;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await _rooms.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<RoomResult>(saved.Error);
        var finalAmenityIds = await _rooms.GetAmenityIdsAsync(room.Id, cancellationToken);
        return Result.Success(PropertyMapping.ToResult(room, finalAmenityIds));
    }
}
