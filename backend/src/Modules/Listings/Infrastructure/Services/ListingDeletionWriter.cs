using HomeMatch.Modules.Listings.Application;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Viewings;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class ListingDeletionWriter(ListingsDbContext db, IViewingCancellationWriter viewings) : IListingDeletionWriter
{
    public async Task<Result> DeleteForRoomAsync(long landlordId, long roomId, DateTimeOffset deletedAt, CancellationToken cancellationToken)
    {
        // Include already deleted listings so a repeated room DELETE retries failed viewing cancellation.
        var listings = await db.Listings.Where(item => item.RoomId == roomId).ToArrayAsync(cancellationToken);
        if (listings.Any(item => item.LandlordId != landlordId)) return Result.Failure(ListingsErrors.ListingNotFound);
        if (listings.Any(item => item.DeletedAt == null && !ListingPublicationPolicy.CanEdit(item.Status)))
            return Result.Failure(ListingsErrors.EditLocked);
        foreach (var listing in listings.Where(item => item.DeletedAt == null))
        {
            listing.DeletedAt = deletedAt;
            listing.UpdatedAt = deletedAt;
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var listing in listings)
            await viewings.CancelForListingAsync(listing.Id, cancellationToken);
        return Result.Success();
    }
}
