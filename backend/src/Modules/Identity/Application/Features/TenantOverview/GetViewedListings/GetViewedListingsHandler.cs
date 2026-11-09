using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetViewedListings;

public sealed class GetViewedListingsHandler
{
    private readonly ITenantListingActivityReader _listingActivityReader;

    public GetViewedListingsHandler(ITenantListingActivityReader listingActivityReader)
    {
        _listingActivityReader = listingActivityReader;
    }

    public async Task<Result<PagedViewedListingsDto>> HandleAsync(
        GetViewedListingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _listingActivityReader.GetViewedListingsPagedAsync(
            query.TenantId,
            page,
            pageSize,
            cancellationToken);

        return Result.Success(new PagedViewedListingsDto(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount));
    }
}
