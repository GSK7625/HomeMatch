namespace HomeMatch.Modules.Properties.Application.Features.Rooms.CreateRoom;

public sealed record CreateRoomCommand(
    long LandlordId,
    long PropertyId,
    string RoomNumber,
    int Floor,
    decimal AreaSqm,
    string RoomType,
    int MaxOccupants,
    DateOnly? AvailableFrom,
    IReadOnlyList<long>? AmenityIds = null);
