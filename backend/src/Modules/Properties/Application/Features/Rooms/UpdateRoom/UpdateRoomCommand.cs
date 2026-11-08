namespace HomeMatch.Modules.Properties.Application.Features.Rooms.UpdateRoom;

public sealed record UpdateRoomCommand(
    long LandlordId,
    long PropertyId,
    long RoomId,
    string? RoomNumber,
    int? Floor,
    decimal? AreaSqm,
    string? RoomType,
    int? MaxOccupants,
    long? BasePrice,
    DateOnly? AvailableFrom,
    IReadOnlyList<long>? AmenityIds = null);
