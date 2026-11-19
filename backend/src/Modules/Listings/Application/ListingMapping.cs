using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingMapping
{
    public static ListingDto ToResult(Listing listing, ListingCoordinates? coordinates) =>
        new(listing.Id, listing.RoomId, listing.LandlordId, listing.Title, listing.Slug,
            listing.Description, listing.Price, listing.DepositAmount, listing.MinLeaseMonths,
            listing.HouseRules, listing.Status,
            coordinates?.Longitude, coordinates?.Latitude,
            listing.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Id).Select(ToImageResult).ToArray(),
            listing.ViewCount,
            new(listing.Revision, listing.ReviewedBy, listing.ReviewedAt, listing.RejectionReason, listing.PublishedAt), listing.ExpiresAt);

    public static ListingSummaryDto ToSummary(Listing listing) =>
        ToSummary(listing, null);

    public static ListingSummaryDto ToSummary(Listing listing, ListingCoordinates? coordinates) =>
        new(listing.Id, listing.RoomId, listing.Title, listing.Slug, listing.Price, listing.Status,
            listing.Images.FirstOrDefault(image => image.IsPrimary)?.ImageUrl ?? listing.Images.FirstOrDefault()?.ImageUrl,
            depositAmount: listing.DepositAmount,
            latitude: coordinates?.Latitude,
            longitude: coordinates?.Longitude);

    public static ListingSummaryDto ToCardSummary(Listing listing, HomeMatch.Shared.Contracts.RoomCardSummary? room) =>
        ToCardSummary(listing, room, null);

    public static ListingSummaryDto ToCardSummary(
        Listing listing,
        HomeMatch.Shared.Contracts.RoomCardSummary? room,
        ListingCoordinates? coordinates) =>
        new(listing.Id, listing.RoomId, listing.Title, listing.Slug, listing.Price, listing.Status,
            listing.Images.FirstOrDefault(image => image.IsPrimary)?.ImageUrl ?? listing.Images.FirstOrDefault()?.ImageUrl,
            depositAmount: listing.DepositAmount,
            areaSqm: room?.AreaSqm,
            roomType: room?.RoomType,
            districtName: room?.DistrictName,
            provinceName: room?.ProvinceName,
            addressDetail: room?.AddressDetail,
            electricityPrice: room?.ElectricityPrice,
            waterPrice: room?.WaterPrice,
            amenities: room?.Amenities,
            latitude: coordinates?.Latitude,
            longitude: coordinates?.Longitude);

    public static ListingImageDto ToImageResult(ListingImage image) =>
        new(image.Id, image.ImageUrl, image.IsPrimary, image.SortOrder,
            image.MediaType, image.ThumbnailUrl, image.DurationSeconds);
}
