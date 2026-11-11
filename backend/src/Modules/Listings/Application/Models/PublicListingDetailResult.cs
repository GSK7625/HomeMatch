using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Models;

public sealed record PublicListingDetailResult(ListingResult Listing, RoomListingDetailSnapshot Room);
