using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;

public static class UpdateListingValidator
{
    public static Error? Validate(UpdateListingCommand command)
    {
        if (command.Title is not null && (string.IsNullOrWhiteSpace(command.Title) || command.Title.Length > 255)
            || command.Description is not null && string.IsNullOrWhiteSpace(command.Description)
            || command.Price is <= 0 || command.DepositAmount is < 0 || command.MinLeaseMonths is <= 0
            || (command.HouseRules is not null && command.HouseRules.Length > 2000))
        {
            return ListingsErrors.InvalidInput;
        }

        return null;
    }
}
