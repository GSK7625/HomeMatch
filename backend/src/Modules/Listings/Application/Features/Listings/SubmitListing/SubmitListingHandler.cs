using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.SubmitListing;

public sealed class SubmitListingHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<ListingDto>> HandleAsync(SubmitListingCommand command, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingDto>(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure<ListingDto>(ListingsErrors.InvalidStatus);
        await listings.ExpirePublishedAsync(DateTimeOffset.UtcNow, listing.RoomId, cancellationToken);
        var room = await rooms.GetRoomAsync(listing.RoomId, cancellationToken);
        ListingCoordinates? coordinates = room is { Longitude: { } longitude, Latitude: { } latitude }
            ? new((double)longitude, (double)latitude) : null;
        var hasRates = await rooms.HasElectricityAndWaterRatesAsync(listing.RoomId, BusinessDate.Today(), cancellationToken);
        var anotherPublished = await listings.HasOtherPublishedAsync(listing.RoomId, listing.Id, cancellationToken);
        var error = ListingPublicationPolicy.ValidateCanSubmit(listing.Status, room, command.LandlordId,
            coordinates is not null, listing.Images.Select(image => image.IsPrimary).ToArray(), hasRates, anotherPublished);
        if (error is not null) return Result.Failure<ListingDto>(error);
        await listings.SetCoordinatesAsync(listing, coordinates!.Longitude, coordinates.Latitude, cancellationToken);
        listing.Status = "PENDING_APPROVAL";
        listing.ReviewedBy = null;
        listing.ReviewedAt = null;
        listing.RejectionReason = null;
        listing.PublishedAt = null;
        listing.ExpiresAt = null;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingDto>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, coordinates));
    }
}
