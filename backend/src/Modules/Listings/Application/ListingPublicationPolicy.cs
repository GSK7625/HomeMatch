using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Modules.Listings.Domain.Entities;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingPublicationPolicy
{
    public static bool CanEdit(string status) => status is "DRAFT" or "HIDDEN" or "REJECTED" or "EXPIRED";

    public static bool IsPublic(Listing? listing, DateTimeOffset now) =>
        listing is { Status: "PUBLISHED" } && (listing.ExpiresAt is null || listing.ExpiresAt > now);

    public static Error? ValidateCanSubmit(
        string listingStatus, RoomListingSnapshot? room, long landlordId,
        bool hasCoordinates, IReadOnlyCollection<bool> imagePrimaryFlags,
        bool hasRequiredRates, bool anotherPublished)
    {
        if (!CanEdit(listingStatus))
            return ListingsErrors.InvalidStatus;
        return ValidateReadiness(room, landlordId, hasCoordinates, imagePrimaryFlags, hasRequiredRates, anotherPublished);
    }

    public static Error? ValidateReadiness(
        RoomListingSnapshot? room, long landlordId, bool hasCoordinates,
        IReadOnlyCollection<bool> imagePrimaryFlags, bool hasRequiredRates, bool anotherPublished)
    {
        if (room is null || room.LandlordId != landlordId || room.Status != "AVAILABLE")
            return ListingsErrors.RoomUnavailable;
        if (!hasCoordinates || imagePrimaryFlags.Count == 0
            || imagePrimaryFlags.Count(isPrimary => isPrimary) != 1)
            return ListingsErrors.Incomplete;
        if (!hasRequiredRates)
            return ListingsErrors.MissingTariffs;
        if (anotherPublished)
            return ListingsErrors.RoomAlreadyPublished;
        return null;
    }

}
