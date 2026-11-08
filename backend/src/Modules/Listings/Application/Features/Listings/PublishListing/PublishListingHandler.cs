using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.PublishListing;

public sealed class PublishListingHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<ListingResult>> HandleAsync(PublishListingCommand command, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        var room = await rooms.GetRoomAsync(listing.RoomId, cancellationToken);
        var coordinates = await listings.GetCoordinatesAsync(listing.Id, cancellationToken);
        var hasRates = await rooms.HasElectricityAndWaterRatesAsync(listing.RoomId, BusinessDate.Today(), cancellationToken);
        var anotherPublished = await listings.HasOtherPublishedAsync(listing.RoomId, listing.Id, cancellationToken);
        var error = ListingPublicationPolicy.ValidateCanPublish(listing.Status, room, command.LandlordId,
            coordinates is not null, listing.Images.Select(image => image.IsPrimary).ToArray(), hasRates, anotherPublished);
        if (error is not null) return Result.Failure<ListingResult>(error);
        listing.Status = "PUBLISHED";
        listing.PublishedAt = DateTimeOffset.UtcNow;
        listing.UpdatedAt = listing.PublishedAt.Value;
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, coordinates));
    }
}
