namespace HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;

public sealed record GetPublicListingByIdQuery(long ListingId, long? ViewerUserId = null, string? IpAddress = null);

