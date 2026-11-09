namespace HomeMatch.Modules.Listings.Application.Features.Moderation.RejectListing;

public sealed record RejectListingCommand(long AdminId, long ListingId, string? Reason);
