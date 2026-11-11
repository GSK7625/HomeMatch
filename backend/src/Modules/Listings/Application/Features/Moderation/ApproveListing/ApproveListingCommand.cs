namespace HomeMatch.Modules.Listings.Application.Features.Moderation.ApproveListing;

public sealed record ApproveListingCommand(long AdminId, long ListingId);
