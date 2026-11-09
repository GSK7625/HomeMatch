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
        var result = new List<ListingSummaryResult>(query.Limit);
        var offset = 0;
        while (result.Count < query.Limit)
        {
            var candidates = await listings.GetPublicCandidatesAsync(offset, 100, cancellationToken);
            if (candidates.Count == 0) break;
            offset += candidates.Count;
            var publicIds = await ListingVisibility.GetPublicIdsAsync(candidates, rooms, cancellationToken);
            result.AddRange(candidates.Where(listing => publicIds.Contains(listing.Id))
                .Take(query.Limit - result.Count).Select(ListingMapping.ToSummary));
        }
        return Result.Success<IReadOnlyList<ListingSummaryResult>>(result);
    }
}
