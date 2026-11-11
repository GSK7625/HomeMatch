namespace HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;

public sealed record SearchListingsQuery(
    long? MinPrice = null,
    long? MaxPrice = null,
    string? ProvinceCode = null,
    string? WardCode = null,
    string? Keyword = null,
    string? RoomType = null,
    int? Occupants = null,
    decimal? MinArea = null,
    decimal? MaxArea = null,
    long[]? AmenityIds = null,
    int Page = 1,
    int PageSize = 20);
