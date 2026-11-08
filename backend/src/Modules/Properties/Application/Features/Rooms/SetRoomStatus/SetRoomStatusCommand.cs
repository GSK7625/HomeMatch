namespace HomeMatch.Modules.Properties.Application.Features.Rooms.SetRoomStatus;

public sealed record SetRoomStatusCommand(
    long LandlordId,
    long PropertyId,
    long RoomId,
    string Status);
