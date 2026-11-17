using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;

public sealed class GetPublicListingByIdHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<PublicListingDetailDto>> HandleAsync(GetPublicListingByIdQuery query, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetByIdAsync(query.ListingId, cancellationToken);
        if (!ListingPublicationPolicy.IsPublic(listing, DateTimeOffset.UtcNow))
            return Result.Failure<PublicListingDetailDto>(ListingsErrors.ListingNotFound);

        var room = await rooms.GetRoomDetailAsync(listing!.RoomId, cancellationToken);
        if (room?.Status != "AVAILABLE")
            return Result.Failure<PublicListingDetailDto>(ListingsErrors.ListingNotFound);

        var coordinates = await listings.GetCoordinatesAsync(query.ListingId, cancellationToken);
        return Result.Success(new PublicListingDetailDto(ListingMapping.ToResult(listing, coordinates), room));
    }
}
