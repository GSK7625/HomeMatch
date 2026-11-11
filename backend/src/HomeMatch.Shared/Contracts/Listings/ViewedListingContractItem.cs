namespace HomeMatch.Shared.Contracts.Listings;

public record ViewedListingContractItem(
    long ViewId,
    long ListingId,
    long RoomId,
    long LandlordId,
    string LandlordName,
    string Title,
    decimal Price,
    decimal AreaSqm,
    string? Address,
    string? PrimaryImageUrl,
    bool IsPublished,
    DateTimeOffset ViewedAt);
