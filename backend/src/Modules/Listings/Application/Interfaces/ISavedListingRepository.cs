using HomeMatch.Modules.Listings.Domain.Entities;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface ISavedListingRepository
{
    Task<bool> ToggleAsync(long userId, long listingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavedListing>> GetForUserAsync(long userId, CancellationToken cancellationToken);
}
