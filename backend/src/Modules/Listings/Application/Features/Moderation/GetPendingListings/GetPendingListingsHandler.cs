using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.GetPendingListings;

public sealed class GetPendingListingsHandler(IListingRepository listings)
{
    public async Task<Result<PendingListingsDto>> HandleAsync(GetPendingListingsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            return Result.Failure<PendingListingsDto>(ListingsErrors.InvalidPage);
        var (items, total) = await listings.GetPendingAsync(query.Page, query.PageSize, cancellationToken);
        return Result.Success(new PendingListingsDto(items.Select(ListingMapping.ToSummary).ToArray(), query.Page, query.PageSize, total));
    }
}
