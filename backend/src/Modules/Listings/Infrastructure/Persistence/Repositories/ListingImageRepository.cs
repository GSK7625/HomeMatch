using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HomeMatch.Modules.Listings.Application;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;

public sealed class ListingImageRepository(ListingsDbContext db) : IListingImageRepository
{
    public async Task<Result> SaveAsync(ListingImage image, bool isNew, CancellationToken cancellationToken)
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
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ListingsErrors.RevisionConflict);
        }
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(ListingImage image, ListingImage? replacement, CancellationToken cancellationToken)
    {
        db.ListingImages.Remove(image);
        if (replacement is not null) replacement.IsPrimary = true;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ListingsErrors.RevisionConflict);
        }
    }
}
