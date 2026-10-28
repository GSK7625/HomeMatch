using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Application;

public sealed class ListingReadService(ListingsDbContext db, IRoomListingReader rooms)
    : IListingReadService
{
    public async Task<IReadOnlyList<ListingSummaryResponse>> GetMineAsync(
        long landlordId, CancellationToken cancellationToken)
    {
        var listings = await db.Listings.AsNoTracking().Include(item => item.Images)
            .Where(item => item.LandlordId == landlordId)
            .OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return listings.Select(ListingResponseMapper.ToSummary).ToArray();
    }

    public async Task<ListingResponse> GetMineByIdAsync(
        long landlordId, long listingId, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == listingId && item.LandlordId == landlordId,
                cancellationToken)
            ?? throw ApiException.NotFound("Listing.NotFound", "Không tìm thấy tin thuộc tài khoản này.");
        return await ListingResponseMapper.ToResponseAsync(db, listing, cancellationToken);
    }

    public async Task<IReadOnlyList<ListingSummaryResponse>> GetPublicAsync(
        int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 50)
            throw ApiException.BadRequest("Listing.InvalidLimit", "Giới hạn phải từ 1 đến 50.");
        var result = new List<ListingSummaryResponse>(limit);
        var offset = 0;
        while (result.Count < limit)
        {
            var listings = await db.Listings.AsNoTracking().Include(item => item.Images)
                .Where(item => item.Status == "PUBLISHED")
                .OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id)
                .Skip(offset).Take(100).ToListAsync(cancellationToken);
            if (listings.Count == 0)
                break;
            offset += listings.Count;
            var snapshots = await rooms.GetRoomsAsync(listings.Select(item => item.RoomId)
                .Distinct().ToArray(), cancellationToken);
            result.AddRange(listings.Where(item => snapshots.TryGetValue(item.RoomId, out var room)
                    && room.Status == "AVAILABLE")
                .Take(limit - result.Count).Select(ListingResponseMapper.ToSummary));
        }
        return result;
    }

    public async Task<ListingResponse> GetPublicByIdAsync(
        long listingId, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == listingId && item.Status == "PUBLISHED",
                cancellationToken)
            ?? throw ApiException.NotFound("Listing.NotFound", "Không tìm thấy tin đang đăng.");
        var room = await rooms.GetRoomAsync(listing.RoomId, cancellationToken);
        if (room?.Status != "AVAILABLE")
            throw ApiException.NotFound("Listing.NotFound", "Không tìm thấy tin đang đăng.");
        return await ListingResponseMapper.ToResponseAsync(db, listing, cancellationToken);
    }
}
