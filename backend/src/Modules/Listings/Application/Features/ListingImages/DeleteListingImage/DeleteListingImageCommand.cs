namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.DeleteListingImage;

public sealed record DeleteListingImageCommand(long LandlordId, long ListingId, long ImageId);

