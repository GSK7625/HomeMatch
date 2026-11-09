using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingImageRepository
{
    Task<Result> SaveAsync(ListingImage image, bool isNew, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(ListingImage image, ListingImage? replacement, CancellationToken cancellationToken);
}
