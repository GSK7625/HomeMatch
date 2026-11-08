using HomeMatch.Modules.Listings.Application;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;

public sealed class ListingRepository(ListingsDbContext db) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken) =>
        db.Listings.Include(item => item.Images).FirstOrDefaultAsync(item => item.Id == listingId, cancellationToken);

    public Task<Listing?> GetOwnedAsync(long landlordId, long listingId, CancellationToken cancellationToken) =>
        db.Listings.Include(item => item.Images).FirstOrDefaultAsync(
            item => item.Id == listingId && item.LandlordId == landlordId, cancellationToken);

    public async Task<IReadOnlyList<Listing>> GetMineAsync(long landlordId, CancellationToken cancellationToken) =>
        await db.Listings.AsNoTracking().Include(item => item.Images)
            .Where(item => item.LandlordId == landlordId)
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Listing>> GetPublicCandidatesAsync(int offset, int limit, CancellationToken cancellationToken) =>
        await db.Listings.AsNoTracking().Include(item => item.Images)
            .Where(item => item.Status == "PUBLISHED")
            .OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);

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
