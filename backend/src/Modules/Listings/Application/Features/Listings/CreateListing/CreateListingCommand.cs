namespace HomeMatch.Modules.Listings.Application.Features.Listings.CreateListing;

public sealed record CreateListingCommand(
    long LandlordId,
    long RoomId,
    string Title,
    string Description,
    long Price,
    long DepositAmount,
    int MinLeaseMonths,
    string? HouseRules);

