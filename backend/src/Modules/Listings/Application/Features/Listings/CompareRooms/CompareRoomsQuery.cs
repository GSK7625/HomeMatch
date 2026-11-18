namespace HomeMatch.Modules.Listings.Application.Features.Listings.CompareRooms;

public sealed record CompareRoomsQuery(IReadOnlyList<long>? RoomIds);
