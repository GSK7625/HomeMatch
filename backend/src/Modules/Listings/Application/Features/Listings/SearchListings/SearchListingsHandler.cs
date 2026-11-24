using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;

public sealed class SearchListingsHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<SearchListingsDto>> HandleAsync(
        SearchListingsQuery query, CancellationToken cancellationToken = default)
    {
        query = query with
        {
            ProvinceCode = Trim(query.ProvinceCode),
            WardCode = Trim(query.WardCode),
            RoomType = Trim(query.RoomType)?.ToUpperInvariant(),
            PropertyType = Trim(query.PropertyType)?.ToUpperInvariant(),
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : System.Text.RegularExpressions.Regex.Replace(query.Keyword.Trim(), @"\s+", " "),
            AmenityIds = query.AmenityIds?.Distinct().ToArray() ?? []
        };
        if (query.Page < 1 || query.PageSize is < 1 or > 100
            || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            return Result.Failure<SearchListingsDto>(ListingsErrors.InvalidPage);
        if (query.MinPrice < 0 || query.MaxPrice < 0 || query.MinPrice > query.MaxPrice
            || query.MinArea <= 0 || query.MaxArea <= 0 || query.MinArea > query.MaxArea
            || query.Occupants <= 0 || query.Keyword?.Length > 200
            || query.ProvinceCode?.Length > 20 || query.WardCode?.Length > 20
            || query.AmenityIds.Length > 50 || query.AmenityIds.Any(id => id <= 0))
            return Result.Failure<SearchListingsDto>(ListingsErrors.InvalidSearch);

        if (query.Latitude.HasValue != query.Longitude.HasValue
            || query.Latitude is { } lat && (!double.IsFinite(lat) || lat < -90 || lat > 90)
            || query.Longitude is { } lon && (!double.IsFinite(lon) || lon < -180 || lon > 180)
            || query.RadiusMeters is <= 0 or > 20000
            || query.RadiusMeters.HasValue && !query.Latitude.HasValue
            || query.PropertyType is not (null or "APARTMENT" or "BOARDING_HOUSE" or "SERVICED_APARTMENT" or "HOMESTAY"))
            return Result.Failure<SearchListingsDto>(ListingsErrors.InvalidSearch);
        if (query.Latitude.HasValue) query = query with { RadiusMeters = query.RadiusMeters ?? 3000 };

        if (query.RoomType is not null && !await rooms.RoomTypeExistsAsync(query.RoomType, cancellationToken))
            return Result.Failure<SearchListingsDto>(ListingsErrors.InvalidSearch);

        var matches = await rooms.SearchAvailableRoomsAsync(new(
            query.ProvinceCode, query.WardCode, query.RoomType, query.Occupants,
            query.MinArea, query.MaxArea, query.AmenityIds, query.Keyword, query.PropertyType), cancellationToken);
        if (matches.Count == 0)
            return Result.Success(new SearchListingsDto([], query.Page, query.PageSize, 0));

        // shortcut: pass matching room IDs in memory; revisit when searches match very large room inventories.
        var roomIds = matches.Select(room => room.RoomId).ToArray();
        var keywordRoomIds = matches.Where(room => room.MatchesKeyword).Select(room => room.RoomId).ToArray();
        var (items, total) = await listings.SearchPublicAsync(query, roomIds, keywordRoomIds, cancellationToken);
        if (items.Count == 0)
            return Result.Success(new SearchListingsDto([], query.Page, query.PageSize, total));

        var pagedRoomIds = items.Select(x => x.Listing.RoomId).Distinct().ToArray();
        var pagedListingIds = items.Select(x => x.Listing.Id).Distinct().ToArray();
        var roomCards = await rooms.GetRoomCardSummariesAsync(pagedRoomIds, cancellationToken);
        var coordsByListing = await listings.GetCoordinatesBatchAsync(pagedListingIds, cancellationToken);

        var summaries = items.Select(item =>
        {
            var listing = item.Listing;
            roomCards.TryGetValue(listing.RoomId, out var roomCard);
            coordsByListing.TryGetValue(listing.Id, out var coords);
            return ListingMapping.ToCardSummary(listing, roomCard, coords, item.DistanceMeters);
        }).ToArray();

        return Result.Success(new SearchListingsDto(summaries, query.Page, query.PageSize, total));
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
