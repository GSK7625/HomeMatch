using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.CreateListing;

public static class CreateListingValidator
{
    public static Error? Validate(CreateListingCommand command) =>
        command.RoomId <= 0 ? ListingsErrors.InvalidInput :
        ListingsValidation.ValidateListing(command.Title, command.Slug, command.Description, command.Price,
            command.DepositAmount, command.MinLeaseMonths, command.HasCurfew, command.CurfewTime)
        ?? ListingsValidation.ValidateCoordinates(command.Longitude, command.Latitude);
}
