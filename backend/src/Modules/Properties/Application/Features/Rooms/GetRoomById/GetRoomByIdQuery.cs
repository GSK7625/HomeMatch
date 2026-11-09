namespace HomeMatch.Modules.Properties.Application.Features.Rooms.GetRoomById;

public sealed record GetRoomByIdQuery(
    long LandlordId,
    long PropertyId,
    long RoomId);
