using HomeMatch.Modules.Listings.Domain.Entities;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingImageRepository
{
    Task SaveAsync(ListingImage image, bool isNew, CancellationToken cancellationToken);
    Task DeleteAsync(ListingImage image, ListingImage? replacement, CancellationToken cancellationToken);
}
