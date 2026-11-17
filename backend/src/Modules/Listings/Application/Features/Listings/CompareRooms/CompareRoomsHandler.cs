using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.CompareRooms;

public sealed class CompareRoomsHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<IReadOnlyList<PublicListingDetailDto>>> HandleAsync(
        CompareRoomsQuery query, CancellationToken cancellationToken = default)
    {
        var ids = query.RoomIds;
        if (ids is null || ids.Count is < 1 or > 3 || ids.Any(id => id <= 0)
            || ids.Distinct().Count() != ids.Count)
            return Result.Failure<IReadOnlyList<PublicListingDetailDto>>(ListingsErrors.InvalidComparison);

        var candidates = await listings.GetPublicByRoomIdsAsync(ids, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var byRoom = candidates.Where(listing => ListingPublicationPolicy.IsPublic(listing, now))
            .GroupBy(listing => listing.RoomId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(listing => listing.Id).First());
        var coordinates = await listings.GetCoordinatesBatchAsync(
            byRoom.Values.Select(listing => listing.Id).ToArray(), cancellationToken);
        var result = new List<PublicListingDetailDto>(ids.Count);
        foreach (var id in ids)
        {
            if (!byRoom.TryGetValue(id, out var listing)) continue;
            var room = await rooms.GetRoomDetailAsync(id, cancellationToken);
            if (room?.Status != "AVAILABLE") continue;
            result.Add(new(ListingMapping.ToResult(listing, coordinates.GetValueOrDefault(listing.Id)), room));
        }

        return Result.Success<IReadOnlyList<PublicListingDetailDto>>(result);
    }
}
