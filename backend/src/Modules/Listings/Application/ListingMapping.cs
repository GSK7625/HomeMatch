using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingMapping
{
    public static ListingResult ToResult(Listing listing, ListingCoordinates? coordinates) =>
        new(listing.Id, listing.RoomId, listing.LandlordId, listing.Title, listing.Slug,
            listing.Description, listing.Price, listing.DepositAmount, listing.MinLeaseMonths,
            listing.AllowPets, listing.HasCurfew, listing.CurfewTime, listing.Status,
            coordinates?.Longitude, coordinates?.Latitude,
            listing.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Id).Select(ToImageResult).ToArray(),
            listing.IsFeatured, listing.ViewCount);

    public static ListingSummaryResult ToSummary(Listing listing) =>
        new(listing.Id, listing.RoomId, listing.Title, listing.Slug, listing.Price, listing.Status,
            listing.Images.FirstOrDefault(image => image.IsPrimary)?.ImageUrl, listing.IsFeatured);

    public static ListingImageResult ToImageResult(ListingImage image) =>
        new(image.Id, image.ImageUrl, image.IsPrimary, image.SortOrder,
            image.MediaType, image.ThumbnailUrl, image.DurationSeconds);
}
