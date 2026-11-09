namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;

public sealed record UpdateListingImageCommand(
    long LandlordId,
    long ListingId,
    long ImageId,
    bool? IsPrimary,
    int? SortOrder,
    string? MediaType,
    string? ThumbnailUrl,
    int? DurationSeconds);

