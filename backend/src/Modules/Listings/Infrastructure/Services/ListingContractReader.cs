using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class ListingContractReader(
    ListingsDbContext db,
    IRoomListingReader rooms) : IListingContractReader
{
    public async Task<ListingContractInfo?> GetListingByIdAsync(long listingId, CancellationToken cancellationToken = default)
    {
        if (listingId <= 0) return null;

        var listing = await db.Listings.AsNoTracking()
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);

        if (listing is null) return null;

        var roomDetail = await rooms.GetRoomDetailAsync(listing.RoomId, cancellationToken);
        var address = roomDetail?.Property is { } p
            ? string.Join(", ", new[] { p.AddressDetail, p.WardName, p.DistrictName, p.ProvinceName }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : null;

        var primaryImage = listing.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
            ?? listing.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl;

        return new ListingContractInfo(
            ListingId: listing.Id,
            RoomId: listing.RoomId,
            LandlordId: listing.LandlordId,
            Title: listing.Title,
            Price: listing.Price,
            Address: address,
            PrimaryImageUrl: primaryImage,
            IsPublished: listing.Status == "PUBLISHED");
    }

    public async Task<IReadOnlyDictionary<long, ListingContractInfo>> GetListingsByIdsAsync(
        IEnumerable<long> listingIds,
        CancellationToken cancellationToken = default)
    {
        var idList = listingIds.Distinct().Where(id => id > 0).ToList();
        if (idList.Count == 0) return new Dictionary<long, ListingContractInfo>();

        var listings = await db.Listings.AsNoTracking()
            .Include(l => l.Images)
            .Where(l => idList.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (listings.Count == 0) return new Dictionary<long, ListingContractInfo>();

        var roomIds = listings.Select(l => l.RoomId).Distinct().ToList();
        var roomCards = await rooms.GetRoomCardSummariesAsync(roomIds, cancellationToken);

        var result = new Dictionary<long, ListingContractInfo>(listings.Count);
        foreach (var listing in listings)
        {
            roomCards.TryGetValue(listing.RoomId, out var room);
            var address = room is not null
                ? string.Join(", ", new[] { room.AddressDetail, room.DistrictName, room.ProvinceName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                : null;

            var primaryImage = listing.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? listing.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl;

            result[listing.Id] = new ListingContractInfo(
                ListingId: listing.Id,
                RoomId: listing.RoomId,
                LandlordId: listing.LandlordId,
                Title: listing.Title,
                Price: listing.Price,
                Address: address,
                PrimaryImageUrl: primaryImage,
                IsPublished: listing.Status == "PUBLISHED");
        }

        return result;
    }

    public async Task<IReadOnlyList<long>> GetListingIdsByLandlordIdAsync(
        long landlordId,
        CancellationToken cancellationToken = default)
    {
        if (landlordId <= 0) return [];

        return await db.Listings.AsNoTracking()
            .Where(l => l.LandlordId == landlordId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<long>> GetListingIdsByRoomIdAsync(
        long roomId,
        CancellationToken cancellationToken = default)
    {
        if (roomId <= 0) return [];

        return await db.Listings.AsNoTracking()
            .Where(l => l.RoomId == roomId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
    }
}
