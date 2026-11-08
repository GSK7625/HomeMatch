using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;

public sealed class GetPublicListingByIdHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<ListingResult>> HandleAsync(GetPublicListingByIdQuery query, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetByIdAsync(query.ListingId, cancellationToken);
        if (!await ListingVisibility.IsPublicAsync(listing, rooms, cancellationToken))
            return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        return Result.Success(ListingMapping.ToResult(listing!, await listings.GetCoordinatesAsync(query.ListingId, cancellationToken)));
    }
}
