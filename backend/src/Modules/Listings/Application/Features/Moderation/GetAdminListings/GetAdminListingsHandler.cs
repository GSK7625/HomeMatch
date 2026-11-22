using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.GetAdminListings;

public sealed record GetAdminListingsQuery(
    string? Status = null,
    string? Keyword = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminListingItemResult(
    long Id,
    long RoomId,
    long LandlordId,
    string LandlordName,
    string Title,
    string Slug,
    long Price,
    long DepositAmount,
    string Status,
    string? CoverImageUrl,
    decimal? AreaSqm,
    string? RoomType,
    string? AddressDetail,
    long ViewCount,
    string? RejectionReason,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedAdminListingsResult(
    IReadOnlyList<AdminListingItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class GetAdminListingsHandler(
    IListingRepository listings,
    IRoomListingReader rooms,
    IIdentityUserReader identityUsers)
{
    public async Task<Result<PagedAdminListingsResult>> HandleAsync(
        GetAdminListingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            return Result.Failure<PagedAdminListingsResult>(ListingsErrors.InvalidPage);

        var (items, totalCount) = await listings.GetAdminListingsAsync(
            query.Status,
            query.Keyword,
            query.Page,
            query.PageSize,
            cancellationToken);

        if (items.Count == 0)
            return Result.Success(new PagedAdminListingsResult([], query.Page, query.PageSize, totalCount));

        var roomIds = items.Select(l => l.RoomId).Distinct().ToArray();
        var roomCards = await rooms.GetRoomCardSummariesAsync(roomIds, cancellationToken);

        var landlordNames = new Dictionary<long, string>();
        foreach (var landlordId in items.Select(l => l.LandlordId).Distinct())
        {
            var info = await identityUsers.GetUserByIdAsync(landlordId, cancellationToken);
            landlordNames[landlordId] = !string.IsNullOrWhiteSpace(info?.FullName)
                ? info.FullName
                : (info?.Email ?? $"Chủ nhà #{landlordId}");
        }

        var resultItems = items.Select(l =>
        {
            roomCards.TryGetValue(l.RoomId, out var room);
            landlordNames.TryGetValue(l.LandlordId, out var landlordName);
            var cover = l.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl;

            return new AdminListingItemResult(
                Id: l.Id,
                RoomId: l.RoomId,
                LandlordId: l.LandlordId,
                LandlordName: landlordName ?? $"Chủ nhà #{l.LandlordId}",
                Title: l.Title,
                Slug: l.Slug,
                Price: l.Price,
                DepositAmount: l.DepositAmount,
                Status: l.Status,
                CoverImageUrl: cover,
                AreaSqm: room?.AreaSqm,
                RoomType: room?.RoomType,
                AddressDetail: room?.AddressDetail,
                ViewCount: l.ViewCount,
                RejectionReason: l.RejectionReason,
                PublishedAt: l.PublishedAt,
                ExpiresAt: l.ExpiresAt,
                CreatedAt: l.CreatedAt,
                UpdatedAt: l.UpdatedAt);
        }).ToArray();

        return Result.Success(new PagedAdminListingsResult(resultItems, query.Page, query.PageSize, totalCount));
    }
}
