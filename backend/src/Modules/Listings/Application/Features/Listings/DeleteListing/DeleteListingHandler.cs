using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Viewings;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.DeleteListing;

public sealed class DeleteListingHandler(IListingRepository listings, IViewingCancellationWriter viewings)
{
    public async Task<Result> HandleAsync(DeleteListingCommand command, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetOwnedForDeletionAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure(ListingsErrors.ListingNotFound);
        if (!listing.DeletedAt.HasValue && !ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure(ListingsErrors.EditLocked);
        if (!listing.DeletedAt.HasValue)
        {
            listing.DeletedAt = DateTimeOffset.UtcNow;
            listing.UpdatedAt = listing.DeletedAt.Value;
            var saved = await listings.SaveChangesAsync(cancellationToken);
            if (saved.IsFailure) return saved;
        }

        // A repeated DELETE also retries cancellation if the other module previously failed.
        await viewings.CancelForListingAsync(listing.Id, cancellationToken);
        return Result.Success();
    }
}
