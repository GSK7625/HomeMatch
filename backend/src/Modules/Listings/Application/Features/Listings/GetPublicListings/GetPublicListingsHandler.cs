using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListings;

public sealed class GetPublicListingsHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<IReadOnlyList<ListingSummaryResult>>> HandleAsync(GetPublicListingsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Limit is < 1 or > 50)
            return Result.Failure<IReadOnlyList<ListingSummaryResult>>(ListingsErrors.InvalidLimit);
        var selectedListings = new List<Listing>(query.Limit);
        var offset = 0;
        while (selectedListings.Count < query.Limit)
        {
            var candidates = await listings.GetPublicCandidatesAsync(offset, 100, cancellationToken);
            if (candidates.Count == 0) break;
            offset += candidates.Count;
            var publicIds = await ListingVisibility.GetPublicIdsAsync(candidates, rooms, cancellationToken);
            selectedListings.AddRange(candidates.Where(listing => publicIds.Contains(listing.Id))
                .Take(query.Limit - selectedListings.Count));
        }

        if (selectedListings.Count == 0)
            return Result.Success<IReadOnlyList<ListingSummaryResult>>([]);

        var roomIds = selectedListings.Select(x => x.RoomId).Distinct().ToArray();
        var listingIds = selectedListings.Select(x => x.Id).Distinct().ToArray();
        var roomCards = await rooms.GetRoomCardSummariesAsync(roomIds, cancellationToken);
        var coordsByListing = await listings.GetCoordinatesBatchAsync(listingIds, cancellationToken);

        var result = selectedListings.Select(listing =>
        {
            roomCards.TryGetValue(listing.RoomId, out var roomCard);
            coordsByListing.TryGetValue(listing.Id, out var coords);
            return ListingMapping.ToCardSummary(listing, roomCard, coords);
        }).ToArray();

        return Result.Success<IReadOnlyList<ListingSummaryResult>>(result);
    }
}
