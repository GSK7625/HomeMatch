using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.ApproveListing;

public sealed class ApproveListingHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<ListingResult>> HandleAsync(ApproveListingCommand command, CancellationToken cancellationToken = default)
    {
        if (command.AdminId <= 0)
            return Result.Failure<ListingResult>(ListingsErrors.InvalidReview);
        var listing = await listings.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        if (listing.Status != "PENDING_APPROVAL") return Result.Failure<ListingResult>(ListingsErrors.InvalidStatus);
        await listings.ExpirePublishedAsync(DateTimeOffset.UtcNow, listing.RoomId, cancellationToken);
        var room = await rooms.GetRoomAsync(listing.RoomId, cancellationToken);
        var coordinates = await listings.GetCoordinatesAsync(listing.Id, cancellationToken);
        var hasRates = await rooms.HasElectricityAndWaterRatesAsync(listing.RoomId, BusinessDate.Today(), cancellationToken);
        var anotherPublished = await listings.HasOtherPublishedAsync(listing.RoomId, listing.Id, cancellationToken);
        var error = ListingPublicationPolicy.ValidateReadiness(room, listing.LandlordId, coordinates is not null,
            listing.Images.Select(image => image.IsPrimary).ToArray(), hasRates, anotherPublished);
        if (error is not null) return Result.Failure<ListingResult>(error);
        var now = DateTimeOffset.UtcNow;
        listing.Status = "PUBLISHED";
        listing.ReviewedBy = command.AdminId;
        listing.ReviewedAt = now;
        listing.RejectionReason = null;
        listing.PublishedAt = now;
        listing.ExpiresAt = now.AddDays(30);
        listing.UpdatedAt = now;
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, coordinates));
    }
}
