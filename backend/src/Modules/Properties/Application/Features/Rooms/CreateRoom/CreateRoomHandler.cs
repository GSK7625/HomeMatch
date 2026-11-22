using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Rooms.CreateRoom;

public sealed class CreateRoomHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomRepository _rooms;
    private readonly ICatalogReader _catalogs;

    public CreateRoomHandler(IPropertyRepository properties, IRoomRepository rooms, ICatalogReader catalogs)
    {
        _properties = properties;
        _rooms = rooms;
        _catalogs = catalogs;
    }

    public async Task<Result<RoomResult>> HandleAsync(CreateRoomCommand command, CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.PropertyNotFound);
        var normalizedType = (command.RoomType ?? string.Empty).Trim().ToUpperInvariant();
        var error = RoomValidation.Validate(command.RoomNumber, command.Floor, command.AreaSqm,
            normalizedType, command.MaxOccupants);
        if (error is not null) return Result.Failure<RoomResult>(error);
        if (!await _catalogs.RoomTypeExistsAsync(normalizedType, activeOnly: true, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.InvalidRoom);
        var number = command.RoomNumber.Trim();
        if (await _rooms.NumberExistsAsync(command.PropertyId, number, null, cancellationToken))
            return Result.Failure<RoomResult>(PropertiesErrors.DuplicateRoomNumber);

        var amenityIds = command.AmenityIds ?? [];
        if (amenityIds.Count > 0)
        {
            if (amenityIds.Any(id => id <= 0) || amenityIds.Distinct().Count() != amenityIds.Count
                || !await _rooms.AmenitiesExistAsync(amenityIds, cancellationToken))
                return Result.Failure<RoomResult>(PropertiesErrors.InvalidAmenities);
        }

        var now = DateTimeOffset.UtcNow;
        var room = new Room
        {
            PropertyId = command.PropertyId,
            RoomNumber = number,
            Floor = command.Floor,
            AreaSqm = command.AreaSqm,
            RoomType = normalizedType,
            MaxOccupants = command.MaxOccupants,
            Status = "AVAILABLE",
            AvailableFrom = command.AvailableFrom ?? BusinessDate.Today(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _rooms.Add(room);
        var saved = await _rooms.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<RoomResult>(saved.Error);

        if (amenityIds.Count > 0)
        {
            await _rooms.ReplaceAmenitiesAsync(room.Id, amenityIds, cancellationToken);
            await _rooms.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(PropertyMapping.ToResult(room, amenityIds));
    }
}
