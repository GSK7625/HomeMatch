using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.DTOs;

public sealed record PublicListingDetailDto(ListingDto Listing, RoomListingDetailSnapshot Room);
