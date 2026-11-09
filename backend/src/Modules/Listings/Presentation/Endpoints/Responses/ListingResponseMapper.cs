using HomeMatch.Modules.Listings.Application.Models;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;

internal static class ListingResponseMapper
{
    public static PublicListingDetailResponse ToResponse(PublicListingDetailResult result)
    {
        var room = result.Room;
        var property = room.Property;
        return new(ToResponse(result.Listing),
            new(room.RoomId, room.RoomNumber, room.Floor, room.AreaSqm, room.RoomType,
                room.MaxOccupants, room.Status, room.AvailableFrom,
                room.Amenities.Select(amenity => new PublicListingAmenityResponse(amenity.Id, amenity.Code, amenity.Name)).ToArray()),
            new(property.Id, property.Name, property.PropertyType, property.ProvinceCode, property.ProvinceName,
                property.WardCode, property.WardName, property.DistrictName, property.AddressDetail),
            room.Tariffs.Select(tariff => new PublicListingTariffResponse(
                tariff.Id, tariff.TariffType, tariff.CalcMethod, tariff.UnitPrice)).ToArray());
    }

    public static ListingResponse ToResponse(ListingResult result, bool includeReview = false) =>
        new(result.Id, result.RoomId, result.LandlordId, result.Title, result.Slug, result.Description,
            result.Price, result.DepositAmount, result.MinLeaseMonths, result.AllowPets, result.HasCurfew,
            result.CurfewTime, result.Status, result.Longitude, result.Latitude,
            result.Images.Select(ToResponse).ToArray(), result.IsFeatured, result.ViewCount,
            includeReview && result.Review is { } review
                ? new(review.Revision, review.ReviewedBy, review.ReviewedAt, review.RejectionReason, review.PublishedAt)
                : null);
    public static ListingImageResponse ToResponse(ListingImageResult result) =>
        new(result.Id, result.ImageUrl, result.IsPrimary, result.SortOrder, result.MediaType, result.ThumbnailUrl, result.DurationSeconds);
    public static ListingSummaryResponse ToResponse(ListingSummaryResult result) =>
        new(result.Id, result.RoomId, result.Title, result.Slug, result.Price, result.Status, result.CoverImageUrl, result.IsFeatured);
    public static IReadOnlyList<ListingSummaryResponse> ToResponse(IReadOnlyList<ListingSummaryResult> results) =>
        results.Select(ToResponse).ToArray();
    public static IReadOnlyList<SavedListingResponse> ToResponse(IReadOnlyList<SavedListingResult> results) =>
        results.Select(result => new SavedListingResponse(result.Id, result.ListingId, ToResponse(result.Listing), result.CreatedAt)).ToArray();
}
