namespace HomeMatch.Modules.Listings.Application.Features.Moderation.GetPendingListings;

public sealed record GetPendingListingsQuery(int Page = 1, int PageSize = 20);
