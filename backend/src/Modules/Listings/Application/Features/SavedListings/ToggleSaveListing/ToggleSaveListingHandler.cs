using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.SavedListings.ToggleSaveListing;

public sealed class ToggleSaveListingHandler(IListingRepository listings, ISavedListingRepository saved, IRoomListingReader rooms)
{
    public async Task<Result<bool>> HandleAsync(ToggleSaveListingCommand command, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetByIdAsync(command.ListingId, cancellationToken);
        if (!await ListingVisibility.IsPublicAsync(listing, rooms, cancellationToken))
            return Result.Failure<bool>(ListingsErrors.ListingNotFound);
        return Result.Success(await saved.ToggleAsync(command.UserId, command.ListingId, cancellationToken));
    }
}
