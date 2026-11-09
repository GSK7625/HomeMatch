using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.SavedListings.GetSavedListings;

public sealed class GetSavedListingsHandler(ISavedListingRepository saved, IRoomListingReader rooms)
{
    public async Task<Result<IReadOnlyList<SavedListingResult>>> HandleAsync(GetSavedListingsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await saved.GetForUserAsync(query.UserId, cancellationToken);
        var publicIds = await ListingVisibility.GetPublicIdsAsync(items.Select(item => item.Listing), rooms, cancellationToken);
        return Result.Success<IReadOnlyList<SavedListingResult>>(items.Where(item => publicIds.Contains(item.ListingId))
            .Select(item => new SavedListingResult(item.Id, item.ListingId, ListingMapping.ToSummary(item.Listing), item.CreatedAt)).ToArray());
    }
}
