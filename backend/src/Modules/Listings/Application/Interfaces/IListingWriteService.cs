using HomeMatch.Modules.Listings.Application.DTOs;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingWriteService
{
    Task<ListingResponse> CreateAsync(long landlordId, CreateListingRequest request,
        CancellationToken cancellationToken);
    Task<ListingResponse> UpdateAsync(long landlordId, long listingId,
        UpdateListingRequest request, CancellationToken cancellationToken);
    Task<ListingResponse> PublishAsync(long landlordId, long listingId,
        CancellationToken cancellationToken);
    Task<ListingResponse> HideAsync(long landlordId, long listingId,
        CancellationToken cancellationToken);
}
