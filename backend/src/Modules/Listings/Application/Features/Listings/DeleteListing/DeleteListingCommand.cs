namespace HomeMatch.Modules.Listings.Application.Features.Listings.DeleteListing;

public sealed record DeleteListingCommand(long LandlordId, long ListingId);
