using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.HideListing;

public sealed class HideListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingResult>> HandleAsync(HideListingCommand command, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        if (listing.Status == "PUBLISHED")
        {
            listing.Status = "HIDDEN";
            listing.UpdatedAt = DateTimeOffset.UtcNow;
            var saved = await listings.SaveChangesAsync(cancellationToken);
            if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        }
        else if (listing.Status != "HIDDEN")
            return Result.Failure<ListingResult>(ListingsErrors.InvalidStatus);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
