namespace HomeMatch.Modules.Properties.Application.Features.Rooms.DeleteRoom;

public sealed record DeleteRoomCommand(long LandlordId, long PropertyId, long RoomId);
