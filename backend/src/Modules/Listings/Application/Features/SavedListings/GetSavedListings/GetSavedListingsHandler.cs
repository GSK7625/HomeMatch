using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.SavedListings.GetSavedListings;

public sealed class GetSavedListingsHandler(ISavedListingRepository saved, IRoomListingReader rooms)
{
    public async Task<Result<IReadOnlyList<SavedListingDto>>> HandleAsync(GetSavedListingsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await saved.GetForUserAsync(query.UserId, cancellationToken);
        var publicIds = await ListingVisibility.GetPublicIdsAsync(items.Select(item => item.Listing), rooms, cancellationToken);
        var visibleItems = items.Where(item => publicIds.Contains(item.ListingId)).ToArray();
        var roomCards = await rooms.GetRoomCardSummariesAsync(
            visibleItems.Select(item => item.Listing.RoomId).Distinct().ToArray(), cancellationToken);
        return Result.Success<IReadOnlyList<SavedListingDto>>(visibleItems
            .Select(item => new SavedListingDto(item.Id, item.ListingId,
                ListingMapping.ToCardSummary(item.Listing, roomCards.GetValueOrDefault(item.Listing.RoomId)), item.CreatedAt)).ToArray());
    }
}
