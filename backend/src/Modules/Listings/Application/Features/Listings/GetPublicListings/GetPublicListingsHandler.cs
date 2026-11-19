using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListings;

public sealed class GetPublicListingsHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<IReadOnlyList<ListingSummaryDto>>> HandleAsync(GetPublicListingsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Limit is < 1 or > 50)
            return Result.Failure<IReadOnlyList<ListingSummaryDto>>(ListingsErrors.InvalidLimit);
        var result = new List<ListingSummaryDto>(query.Limit);
        var offset = 0;
        while (result.Count < query.Limit)
        {
            var candidates = await listings.GetPublicCandidatesAsync(offset, 100, cancellationToken);
            if (candidates.Count == 0) break;
            offset += candidates.Count;
            var publicIds = await ListingVisibility.GetPublicIdsAsync(candidates, rooms, cancellationToken);
            var approved = candidates.Where(listing => publicIds.Contains(listing.Id))
                .Take(query.Limit - result.Count).ToList();
            if (approved.Count == 0) continue;

            var approvedListingIds = approved.Select(a => a.Id).ToArray();
            var approvedRoomIds = approved.Select(a => a.RoomId).Distinct().ToArray();
            var coords = await listings.GetCoordinatesBatchAsync(approvedListingIds, cancellationToken);
            var roomCards = await rooms.GetRoomCardSummariesAsync(approvedRoomIds, cancellationToken);

            result.AddRange(approved.Select(listing =>
            {
                roomCards.TryGetValue(listing.RoomId, out var roomCard);
                coords.TryGetValue(listing.Id, out var coord);
                return ListingMapping.ToCardSummary(listing, roomCard, coord);
            }));
        }
        return Result.Success<IReadOnlyList<ListingSummaryDto>>(result);
    }
}
