using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingPublicationPolicy
{
    public static Error? ValidateCanPublish(
        string listingStatus, RoomListingSnapshot? room, long landlordId,
        bool hasCoordinates, IReadOnlyCollection<bool> imagePrimaryFlags,
        bool hasRequiredRates, bool anotherPublished)
    {
        if (listingStatus is not ("DRAFT" or "HIDDEN"))
            return ListingsErrors.InvalidStatus;
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

    public static void EnsureCanPublish(
        string listingStatus, RoomListingSnapshot? room, long landlordId,
        bool hasCoordinates, IReadOnlyCollection<bool> imagePrimaryFlags,
        bool hasRequiredRates, bool anotherPublished)
    {
        var error = ValidateCanPublish(listingStatus, room, landlordId, hasCoordinates, imagePrimaryFlags, hasRequiredRates, anotherPublished);
        if (error is not null)
        {
            if (error.Code == "Listing.Incomplete" || error.Code == "Listing.MissingTariffs")
                throw ApiException.BadRequest(error.Code, error.Message);
            throw ApiException.Conflict(error.Code, error.Message);
        }
    }
}
