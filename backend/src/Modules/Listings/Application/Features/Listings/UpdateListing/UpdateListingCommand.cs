namespace HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;

public sealed record UpdateListingCommand(
    long LandlordId,
    long ListingId,
    string? Title,
    string? Description,
    long? Price,
    long? DepositAmount,
    int? MinLeaseMonths,
    string? HouseRules);

