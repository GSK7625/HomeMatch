using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.EntityFrameworkCore;
using HomeMatch.Modules.Listings.Application;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class TenantListingActivityReader(
    ListingsDbContext db,
    IRoomListingReader roomReader,
    IIdentityUserReader identityUserReader) : ITenantListingActivityReader
{
    public async Task<int> GetTotalViewedListingsCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        return await db.ListingViews
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .Select(v => v.ListingId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalSavedListingsCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        return await db.SavedListings
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Listing.DeletedAt == null && s.Listing.Status == "PUBLISHED")
            .CountAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ViewedListingContractItem> Items, int TotalCount)> GetViewedListingsPagedAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return ([], 0);
        }

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 100 ? 12 : pageSize;

        var allUserViews = await db.ListingViews
            .AsNoTracking()
            .Include(v => v.Listing)
            .ThenInclude(l => l.Images)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.ViewedAt)
            .ThenByDescending(v => v.Id)
            .ToListAsync(cancellationToken);

        var latestViewsPerListing = allUserViews
            .GroupBy(v => v.ListingId)
            .Select(g => g.First())
            .OrderByDescending(v => v.ViewedAt)
            .ThenByDescending(v => v.Id)
            .ToList();

        var totalCount = latestViewsPerListing.Count;
        var pagedViews = latestViewsPerListing
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var items = new List<ViewedListingContractItem>(pagedViews.Count);
        var landlordNameCache = new Dictionary<long, string>();

        foreach (var view in pagedViews)
        {
            var listing = view.Listing;
            var roomDetail = await roomReader.GetRoomDetailAsync(listing.RoomId, cancellationToken);

            if (!landlordNameCache.TryGetValue(listing.LandlordId, out var landlordName))
            {
                var landlordInfo = await identityUserReader.GetUserByIdAsync(listing.LandlordId, cancellationToken);
                landlordName = landlordInfo?.FullName ?? $"Chủ trọ #{listing.LandlordId}";
                landlordNameCache[listing.LandlordId] = landlordName;
            }

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

            var primaryImageUrl = ListingMapping.CoverImageUrl(listing);

            var isPublished = listing.DeletedAt == null && listing.Status == "PUBLISHED"
                && (listing.ExpiresAt == null || listing.ExpiresAt > DateTimeOffset.UtcNow)
                && (roomDetail is { Status: "AVAILABLE", DeletedAt: null });

            items.Add(new ViewedListingContractItem(
                ViewId: view.Id,
                ListingId: listing.Id,
                RoomId: listing.RoomId,
                LandlordId: listing.LandlordId,
                LandlordName: landlordName,
                Title: listing.Title,
                Price: listing.Price,
                AreaSqm: roomDetail?.AreaSqm ?? 25m,
                Address: address,
                PrimaryImageUrl: primaryImageUrl,
                IsPublished: isPublished,
                ViewedAt: view.ViewedAt));
        }

        return (items, totalCount);
    }
}
