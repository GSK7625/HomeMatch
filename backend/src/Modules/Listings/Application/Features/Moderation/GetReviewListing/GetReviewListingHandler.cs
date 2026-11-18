using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.GetReviewListing;

public sealed class GetReviewListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingDto>> HandleAsync(GetReviewListingQuery query, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetByIdAsync(query.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingDto>(ListingsErrors.ListingNotFound);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
