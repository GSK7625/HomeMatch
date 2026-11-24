namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.UploadListingImage;

public sealed record UploadListingImageCommand(
    long LandlordId, long ListingId, Stream Content, long Length,
    string FileName, string ContentType, bool IsPrimary = false, int SortOrder = 0);
