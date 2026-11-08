namespace HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;

public sealed record UpdateListingCommand(
    long LandlordId,
    long ListingId,
    string? Title,
    string? Slug,
    string? Description,
    long? Price,
    long? DepositAmount,
    int? MinLeaseMonths,
    bool? AllowPets,
    bool? HasCurfew,
    TimeOnly? CurfewTime,
    bool? IsFeatured,
    double? Longitude,
    double? Latitude);

