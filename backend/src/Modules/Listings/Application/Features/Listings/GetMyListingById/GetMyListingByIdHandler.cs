using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetMyListingById;

public sealed class GetMyListingByIdHandler(IListingRepository listings)
{
    public async Task<Result<ListingResult>> HandleAsync(GetMyListingByIdQuery query, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetOwnedAsync(query.LandlordId, query.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
