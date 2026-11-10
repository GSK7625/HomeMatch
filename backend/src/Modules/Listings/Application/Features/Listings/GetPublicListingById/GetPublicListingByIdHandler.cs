using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;

public sealed class GetPublicListingByIdHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<PublicListingDetailResult>> HandleAsync(GetPublicListingByIdQuery query, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetByIdAsync(query.ListingId, cancellationToken);
        if (!ListingPublicationPolicy.IsPublic(listing, DateTimeOffset.UtcNow))
            return Result.Failure<PublicListingDetailResult>(ListingsErrors.ListingNotFound);

        var room = await rooms.GetRoomDetailAsync(listing!.RoomId, cancellationToken);
        if (room?.Status != "AVAILABLE")
            return Result.Failure<PublicListingDetailResult>(ListingsErrors.ListingNotFound);

        var coordinates = await listings.GetCoordinatesAsync(query.ListingId, cancellationToken);
        return Result.Success(new PublicListingDetailResult(ListingMapping.ToResult(listing, coordinates), room));
    }
}
