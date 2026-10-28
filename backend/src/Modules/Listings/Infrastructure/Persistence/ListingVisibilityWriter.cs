using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence;

public sealed class ListingVisibilityWriter(ListingsDbContext db) : IListingVisibilityWriter
{
    public async Task HidePublishedForRoomAsync(long roomId, CancellationToken cancellationToken)
    {
        await db.Listings.Where(listing => listing.RoomId == roomId
                && listing.Status == "PUBLISHED")
            .ExecuteUpdateAsync(update => update
                .SetProperty(listing => listing.Status, "HIDDEN")
                .SetProperty(listing => listing.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }
}
