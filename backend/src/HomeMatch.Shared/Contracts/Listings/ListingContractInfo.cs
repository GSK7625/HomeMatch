namespace HomeMatch.Shared.Contracts.Listings;

public record ListingContractInfo(
    long ListingId,
    long RoomId,
    long LandlordId,
    string Title,
    decimal Price,
    string? Address,
    string? PrimaryImageUrl,
    bool IsPublished);
