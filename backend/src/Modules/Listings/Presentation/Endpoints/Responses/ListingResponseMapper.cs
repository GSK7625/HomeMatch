using HomeMatch.Modules.Listings.Application.Models;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;

internal static class ListingResponseMapper
{
    public static ListingResponse ToResponse(ListingResult result) =>
        new(result.Id, result.RoomId, result.LandlordId, result.Title, result.Slug, result.Description,
            result.Price, result.DepositAmount, result.MinLeaseMonths, result.AllowPets, result.HasCurfew,
            result.CurfewTime, result.Status, result.Longitude, result.Latitude,
            result.Images.Select(ToResponse).ToArray(), result.IsFeatured, result.ViewCount);
    public static ListingImageResponse ToResponse(ListingImageResult result) =>
        new(result.Id, result.ImageUrl, result.IsPrimary, result.SortOrder, result.MediaType, result.ThumbnailUrl, result.DurationSeconds);
    public static ListingSummaryResponse ToResponse(ListingSummaryResult result) =>
        new(result.Id, result.RoomId, result.Title, result.Slug, result.Price, result.Status, result.CoverImageUrl, result.IsFeatured);
    public static IReadOnlyList<ListingSummaryResponse> ToResponse(IReadOnlyList<ListingSummaryResult> results) =>
        results.Select(ToResponse).ToArray();
    public static IReadOnlyList<SavedListingResponse> ToResponse(IReadOnlyList<SavedListingResult> results) =>
        results.Select(result => new SavedListingResponse(result.Id, result.ListingId, ToResponse(result.Listing), result.CreatedAt)).ToArray();
}
