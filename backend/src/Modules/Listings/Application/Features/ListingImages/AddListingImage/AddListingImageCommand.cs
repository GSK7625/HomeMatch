namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;

public sealed record AddListingImageCommand(
    long LandlordId,
    long ListingId,
    string ImageUrl,
    bool IsPrimary,
    int SortOrder,
    string MediaType = "IMAGE",
    string? ThumbnailUrl = null,
    int? DurationSeconds = null);

