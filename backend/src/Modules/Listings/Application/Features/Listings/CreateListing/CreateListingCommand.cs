namespace HomeMatch.Modules.Listings.Application.Features.Listings.CreateListing;

public sealed record CreateListingCommand(
    long LandlordId,
    long RoomId,
    string Title,
    string Slug,
    string Description,
    long Price,
    long DepositAmount,
    int MinLeaseMonths,
    bool AllowPets,
    bool HasCurfew,
    TimeOnly? CurfewTime,
    bool IsFeatured,
    double? Longitude,
    double? Latitude);

