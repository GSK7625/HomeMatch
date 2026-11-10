using HomeMatch.Modules.Listings.Application;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;

public sealed class ListingRepository(ListingsDbContext db) : IListingRepository
{
    public Task<int> ExpirePublishedAsync(DateTimeOffset now, long? roomId, CancellationToken cancellationToken)
    {
        var query = db.Listings.Where(listing => listing.Status == "PUBLISHED" && listing.ExpiresAt <= now);
        if (roomId.HasValue)
            query = query.Where(listing => listing.RoomId == roomId.Value);
        return query.ExecuteUpdateAsync(update => update
            .SetProperty(listing => listing.Status, "EXPIRED")
            .SetProperty(listing => listing.Revision, listing => listing.Revision + 1)
            .SetProperty(listing => listing.UpdatedAt, now), cancellationToken);
    }

    public async Task<(IReadOnlyList<Listing> Items, int TotalCount)> SearchPublicAsync(
        SearchListingsQuery filter, long[] roomIds, long[] keywordRoomIds, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = db.Listings.AsNoTracking()
            .Where(listing => listing.Status == "PUBLISHED" && roomIds.Contains(listing.RoomId)
                && (listing.ExpiresAt == null || listing.ExpiresAt > now));
        if (filter.MinPrice.HasValue)
            query = query.Where(listing => listing.Price >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue)
            query = query.Where(listing => listing.Price <= filter.MaxPrice.Value);
        if (filter.Keyword is not null)
        {
            var pattern = "%" + filter.Keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(listing => keywordRoomIds.Contains(listing.RoomId)
                || EF.Functions.ILike(listing.Title, pattern, "\\")
                || EF.Functions.ILike(listing.Description, pattern, "\\"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Include(listing => listing.Images)
            .OrderByDescending(listing => listing.PublishedAt).ThenByDescending(listing => listing.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .ToArrayAsync(cancellationToken);
        return (items, total);
    }

    public Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken) =>
        db.Listings.Include(item => item.Images).FirstOrDefaultAsync(item => item.Id == listingId, cancellationToken);

    public Task<Listing?> GetOwnedAsync(long landlordId, long listingId, CancellationToken cancellationToken) =>
        db.Listings.Include(item => item.Images).FirstOrDefaultAsync(
            item => item.Id == listingId && item.LandlordId == landlordId, cancellationToken);

    public async Task<IReadOnlyList<Listing>> GetMineAsync(long landlordId, CancellationToken cancellationToken) =>
        await db.Listings.AsNoTracking().Include(item => item.Images)
            .Where(item => item.LandlordId == landlordId)
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pending = db.Listings.AsNoTracking().Where(item => item.Status == "PENDING_APPROVAL");
        var total = await pending.CountAsync(cancellationToken);
        var items = await pending.Include(item => item.Images).OrderBy(item => item.UpdatedAt).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Listing>> GetPublicCandidatesAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.Listings.AsNoTracking().Include(item => item.Images)
            .Where(item => item.Status == "PUBLISHED" && (item.ExpiresAt == null || item.ExpiresAt > now))
            .OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, long? exceptId, CancellationToken cancellationToken) =>
        db.Listings.AnyAsync(item => item.Slug == slug && (!exceptId.HasValue || item.Id != exceptId.Value), cancellationToken);

    public Task<bool> HasOtherPublishedAsync(long roomId, long listingId, CancellationToken cancellationToken) =>
        db.Listings.AnyAsync(item => item.RoomId == roomId && item.Id != listingId && item.Status == "PUBLISHED", cancellationToken);

    public async Task<ListingCoordinates?> GetCoordinatesAsync(long listingId, CancellationToken cancellationToken)
    {
        // Read the point before X/Y: PostGIS ST_X/ST_Y accept geometry, not geography.
        var point = await db.ListingLocations.AsNoTracking().Where(item => item.ListingId == listingId)
            .Select(item => item.Coordinates).FirstOrDefaultAsync(cancellationToken);
        return point is null ? null : new ListingCoordinates(point.X, point.Y);
    }

    public async Task<IReadOnlyDictionary<long, ListingCoordinates>> GetCoordinatesBatchAsync(
        IReadOnlyCollection<long> listingIds, CancellationToken cancellationToken)
    {
        if (listingIds.Count == 0) return new Dictionary<long, ListingCoordinates>();
        var points = await db.ListingLocations.AsNoTracking()
            .Where(item => listingIds.Contains(item.ListingId))
            .Select(item => new { item.ListingId, item.Coordinates })
            .ToListAsync(cancellationToken);
        return points.ToDictionary(p => p.ListingId, p => new ListingCoordinates(p.Coordinates.X, p.Coordinates.Y));
    }

    public async Task SetCoordinatesAsync(Listing listing, double longitude, double latitude, CancellationToken cancellationToken)
    {
        var location = listing.Id == 0 ? null : await db.ListingLocations.FindAsync([listing.Id], cancellationToken);
        var point = new Point(longitude, latitude) { SRID = 4326 };
        if (location is null)
            db.ListingLocations.Add(new ListingLocation { Listing = listing, Coordinates = point });
        else
            location.Coordinates = point;
    }

    public void Add(Listing listing) => db.Listings.Add(listing);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ListingsErrors.RevisionConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_listings_slug" })
        {
            return Result.Failure(ListingsErrors.DuplicateSlug);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_listings_published_room" })
        {
            return Result.Failure(ListingsErrors.RoomAlreadyPublished);
        }
    }
}
