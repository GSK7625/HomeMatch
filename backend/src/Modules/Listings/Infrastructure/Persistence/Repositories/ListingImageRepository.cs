using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;

public sealed class ListingImageRepository(ListingsDbContext db) : IListingImageRepository
{
    public async Task SaveAsync(ListingImage image, bool isNew, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (image.IsPrimary)
        {
            await db.ListingImages.Where(item => item.ListingId == image.ListingId && item.Id != image.Id && item.IsPrimary)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsPrimary, false), cancellationToken);
            // ExecuteUpdate bypasses the tracker; keep loaded images consistent with the database.
            foreach (var tracked in db.ChangeTracker.Entries<ListingImage>()
                         .Where(entry => entry.Entity.ListingId == image.ListingId && entry.Entity.Id != image.Id))
            {
                tracked.Entity.IsPrimary = false;
                tracked.Property(item => item.IsPrimary).OriginalValue = false;
            }
        }
        if (isNew) db.ListingImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(ListingImage image, ListingImage? replacement, CancellationToken cancellationToken)
    {
        db.ListingImages.Remove(image);
        if (replacement is not null) replacement.IsPrimary = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
