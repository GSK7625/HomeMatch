using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;

public sealed class SearchListingsHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<SearchListingsResult>> HandleAsync(
        SearchListingsQuery query, CancellationToken cancellationToken = default)
    {
        query = query with
        {
            ProvinceCode = Trim(query.ProvinceCode),
            WardCode = Trim(query.WardCode),
            Keyword = Trim(query.Keyword),
            RoomType = Trim(query.RoomType)?.ToUpperInvariant(),
            AmenityIds = query.AmenityIds?.Distinct().ToArray() ?? []
        };
        if (query.Page < 1 || query.PageSize is < 1 or > 100
            || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            return Result.Failure<SearchListingsResult>(ListingsErrors.InvalidPage);
        if (query.MinPrice < 0 || query.MaxPrice < 0 || query.MinPrice > query.MaxPrice
            || query.MinArea <= 0 || query.MaxArea <= 0 || query.MinArea > query.MaxArea
            || query.Occupants <= 0 || query.Keyword?.Length > 200
            || query.ProvinceCode?.Length > 20 || query.WardCode?.Length > 20
            || query.AmenityIds.Length > 50 || query.AmenityIds.Any(id => id <= 0)
            || query.RoomType is not (null or "SINGLE" or "STUDIO" or "1B1B" or "2B1B" or "DORMITORY"))
            return Result.Failure<SearchListingsResult>(ListingsErrors.InvalidSearch);

        var matches = await rooms.SearchAvailableRoomsAsync(new(
            query.ProvinceCode, query.WardCode, query.RoomType, query.Occupants,
            query.MinArea, query.MaxArea, query.AmenityIds, query.Keyword), cancellationToken);
        if (matches.Count == 0)
            return Result.Success(new SearchListingsResult([], query.Page, query.PageSize, 0));

        // shortcut: pass matching room IDs in memory; revisit when searches match very large room inventories.
        var roomIds = matches.Select(room => room.RoomId).ToArray();
        var keywordRoomIds = matches.Where(room => room.MatchesKeyword).Select(room => room.RoomId).ToArray();
        var (items, total) = await listings.SearchPublicAsync(query, roomIds, keywordRoomIds, cancellationToken);
        if (items.Count == 0)
            return Result.Success(new SearchListingsResult([], query.Page, query.PageSize, total));

        var pagedRoomIds = items.Select(x => x.RoomId).Distinct().ToArray();
        var roomCards = await rooms.GetRoomCardSummariesAsync(pagedRoomIds, cancellationToken);

        var summaries = items.Select(listing =>
        {
            roomCards.TryGetValue(listing.RoomId, out var roomCard);
            return ListingMapping.ToCardSummary(listing, roomCard);
        }).ToArray();

        return Result.Success(new SearchListingsResult(summaries, query.Page, query.PageSize, total));
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
