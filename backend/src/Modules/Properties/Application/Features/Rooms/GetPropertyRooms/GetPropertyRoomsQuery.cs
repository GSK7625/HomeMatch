namespace HomeMatch.Modules.Properties.Application.Features.Rooms.GetPropertyRooms;

public sealed record GetPropertyRoomsQuery(
    long LandlordId,
    long PropertyId);
