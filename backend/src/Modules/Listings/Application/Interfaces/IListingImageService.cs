using HomeMatch.Modules.Listings.Application.DTOs;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingImageService
{
    Task<ListingImageResponse> AddImageAsync(long landlordId, long listingId,
        AddListingImageRequest request, CancellationToken cancellationToken);
    Task<ListingImageResponse> UpdateImageAsync(long landlordId, long listingId,
        long imageId, UpdateListingImageRequest request, CancellationToken cancellationToken);
    Task DeleteImageAsync(long landlordId, long listingId, long imageId,
        CancellationToken cancellationToken);
}
