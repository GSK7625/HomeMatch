using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingPublicationPolicy
{
    public static void EnsureCanPublish(
        string listingStatus, RoomListingSnapshot? room, long landlordId,
        bool hasCoordinates, IReadOnlyCollection<bool> imagePrimaryFlags,
        bool hasRequiredRates, bool anotherPublished)
    {
        if (listingStatus is not ("DRAFT" or "HIDDEN"))
            throw ApiException.Conflict("Listing.InvalidStatus", "Chỉ có thể đăng tin nháp hoặc tin đang ẩn.");
        if (room is null || room.LandlordId != landlordId || room.Status != "AVAILABLE")
            throw ApiException.Conflict("Listing.RoomUnavailable", "Phòng phải còn trống và thuộc chủ nhà.");
        if (!hasCoordinates || imagePrimaryFlags.Count == 0
            || imagePrimaryFlags.Count(isPrimary => isPrimary) != 1)
            throw ApiException.BadRequest("Listing.Incomplete", "Tin cần tọa độ và ít nhất một ảnh đại diện.");
        if (!hasRequiredRates)
            throw ApiException.BadRequest("Listing.MissingTariffs", "Cần có đơn giá điện và nước đang hiệu lực.");
        if (anotherPublished)
            throw ApiException.Conflict("Listing.RoomAlreadyPublished", "Phòng đã có tin đang đăng.");
    }
}
