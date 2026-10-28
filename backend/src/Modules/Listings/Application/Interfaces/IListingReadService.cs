using HomeMatch.Modules.Listings.Application.DTOs;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingReadService
{
    Task<IReadOnlyList<ListingSummaryResponse>> GetMineAsync(long landlordId,
        CancellationToken cancellationToken);
    Task<ListingResponse> GetMineByIdAsync(long landlordId, long listingId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingSummaryResponse>> GetPublicAsync(int limit,
        CancellationToken cancellationToken);
    Task<ListingResponse> GetPublicByIdAsync(long listingId,
        CancellationToken cancellationToken);
}
