using HomeMatch.Modules.Listings.Domain.Entities;
namespace HomeMatch.Modules.Listings.Application.DTOs;
public sealed record ListingSearchItemDto(Listing Listing, double? DistanceMeters = null);
