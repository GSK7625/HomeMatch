using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetMyListings;

public sealed class GetMyListingsHandler(IListingRepository listings)
{
    public async Task<Result<IReadOnlyList<ListingSummaryResult>>> HandleAsync(GetMyListingsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await listings.GetMineAsync(query.LandlordId, cancellationToken);
        return Result.Success<IReadOnlyList<ListingSummaryResult>>(items.Select(ListingMapping.ToSummary).ToArray());
    }
}
