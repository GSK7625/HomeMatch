using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingVisibility
{
    public static async Task<bool> IsPublicAsync(Listing? listing, IRoomListingReader rooms, CancellationToken cancellationToken)
    {
        if (!ListingPublicationPolicy.IsPublic(listing, DateTimeOffset.UtcNow)) return false;
        var room = await rooms.GetRoomAsync(listing!.RoomId, cancellationToken);
        return room?.Status == "AVAILABLE";
    }

    public static async Task<HashSet<long>> GetPublicIdsAsync(IEnumerable<Listing> listings,
        IRoomListingReader rooms, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = listings.Where(listing => ListingPublicationPolicy.IsPublic(listing, now)).ToArray();
        if (candidates.Length == 0) return [];
        var snapshots = await rooms.GetRoomsAsync(candidates.Select(listing => listing.RoomId).Distinct().ToArray(), cancellationToken);
        return candidates.Where(listing => snapshots.TryGetValue(listing.RoomId, out var room) && room.Status == "AVAILABLE")
            .Select(listing => listing.Id).ToHashSet();
    }
}
