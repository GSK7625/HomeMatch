using HomeMatch.Modules.Listings.Application.DTOs;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;

internal static class ListingResponseMapper
{
    public static PublicListingDetailResponse ToResponse(PublicListingDetailDto result)
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

    public static ListingResponse ToResponse(ListingDto result, bool includeReview = false) =>
        new(result.Id, result.RoomId, result.LandlordId, result.Title, result.Slug, result.Description,
            result.Price, result.DepositAmount, result.MinLeaseMonths, result.HouseRules,
            result.Status, result.Longitude, result.Latitude,
            result.Images.Select(ToResponse).ToArray(), result.ViewCount,
            includeReview && result.Review is { } review
                ? new(review.Revision, review.ReviewedBy, review.ReviewedAt, review.RejectionReason, review.PublishedAt)
                : null, result.ExpiresAt);
    public static ListingImageResponse ToResponse(ListingImageDto result) =>
        new(result.Id, result.ImageUrl, result.IsPrimary, result.SortOrder, result.MediaType, result.ThumbnailUrl, result.DurationSeconds);
    public static ListingSummaryResponse ToResponse(ListingSummaryDto result) =>
        new(result.Id, result.RoomId, result.Title, result.Slug, result.Price, result.Status, result.CoverImageUrl,
            result.DepositAmount, result.AreaSqm, result.RoomType,
            result.DistrictName, result.ProvinceName, result.AddressDetail, result.ElectricityPrice, result.WaterPrice, result.Amenities,
            result.Latitude, result.Longitude);
    public static IReadOnlyList<ListingSummaryResponse> ToResponse(IReadOnlyList<ListingSummaryDto> results) =>
        results.Select(ToResponse).ToArray();
    public static IReadOnlyList<SavedListingResponse> ToResponse(IReadOnlyList<SavedListingDto> results) =>
        results.Select(result => new SavedListingResponse(result.Id, result.ListingId, ToResponse(result.Listing), result.CreatedAt)).ToArray();
}
