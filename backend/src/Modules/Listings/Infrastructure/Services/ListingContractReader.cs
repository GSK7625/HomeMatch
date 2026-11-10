using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class ListingContractReader(
    ListingsDbContext db,
    IRoomListingReader roomReader) : IListingContractReader
{
    public async Task<ListingContractInfo?> GetListingByIdAsync(long listingId, CancellationToken cancellationToken = default)
    {
        if (listingId <= 0)
        {
            return null;
        }

        var listing = await db.Listings
            .AsNoTracking()
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);

        if (listing is null)
        {
            return null;
        }

        var roomDetail = await roomReader.GetRoomDetailAsync(listing.RoomId, cancellationToken);
        return MapToContractInfo(listing, roomDetail);
    }

    public async Task<IReadOnlyDictionary<long, ListingContractInfo>> GetListingsByIdsAsync(
        IEnumerable<long> listingIds,
        CancellationToken cancellationToken = default)
    {
        var ids = listingIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, ListingContractInfo>();
        }

        var listings = await db.Listings
            .AsNoTracking()
            .Include(l => l.Images)
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<long, ListingContractInfo>(listings.Count);
        foreach (var listing in listings)
        {
            var roomDetail = await roomReader.GetRoomDetailAsync(listing.RoomId, cancellationToken);
            result[listing.Id] = MapToContractInfo(listing, roomDetail);
        }

        return result;
    }

    public async Task<IReadOnlyList<long>> GetListingIdsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default)
    {
        if (landlordId <= 0)
        {
            return [];
        }

        return await db.Listings
            .AsNoTracking()
            .Where(l => l.LandlordId == landlordId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<long>> GetListingIdsByRoomIdAsync(long roomId, CancellationToken cancellationToken = default)
    {
        if (roomId <= 0)
        {
            return [];
        }

        return await db.Listings
            .AsNoTracking()
            .Where(l => l.RoomId == roomId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    private static ListingContractInfo MapToContractInfo(Listing listing, RoomListingDetailSnapshot? roomDetail)
    {
        string? address = null;
        if (roomDetail?.Property is { } prop)
        {
            address = string.Join(", ", new[]
            {
                prop.AddressDetail,
                prop.WardName,
                prop.DistrictName,
                prop.ProvinceName
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        var primaryImageUrl = listing.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .FirstOrDefault()?.ImageUrl;

        var isPublished = listing.Status == "PUBLISHED"
            && (roomDetail is null || roomDetail.Status == "AVAILABLE");

        return new ListingContractInfo(
            ListingId: listing.Id,
            RoomId: listing.RoomId,
            LandlordId: listing.LandlordId,
            Title: listing.Title,
            Price: listing.Price,
            Address: address,
            PrimaryImageUrl: primaryImageUrl,
            IsPublished: isPublished);
    }
}
