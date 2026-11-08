using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;

public sealed class SavedListingRepository(ListingsDbContext db) : ISavedListingRepository
{
    public async Task<bool> ToggleAsync(long userId, long listingId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize toggles for this pair across all API instances, not just this process.
        var lockKey = $"listings:saved:{userId}:{listingId}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
        var existing = await db.SavedListings.FirstOrDefaultAsync(
            item => item.UserId == userId && item.ListingId == listingId, cancellationToken);
        if (existing is null)
            db.SavedListings.Add(new SavedListing { UserId = userId, ListingId = listingId, CreatedAt = DateTimeOffset.UtcNow });
        else
            db.SavedListings.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return existing is null;
    }

    public async Task<IReadOnlyList<SavedListing>> GetForUserAsync(long userId, CancellationToken cancellationToken) =>
        await db.SavedListings.AsNoTracking().Include(item => item.Listing).ThenInclude(item => item.Images)
            .Where(item => item.UserId == userId && item.Listing.Status == "PUBLISHED")
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).ToListAsync(cancellationToken);
}
