using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.GetReviewListing;

public sealed class GetReviewListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingResult>> HandleAsync(GetReviewListingQuery query, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetByIdAsync(query.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
