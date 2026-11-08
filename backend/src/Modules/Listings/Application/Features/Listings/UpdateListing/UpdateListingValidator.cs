using System.Text.RegularExpressions;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;

public static class UpdateListingValidator
{
    public static Error? Validate(UpdateListingCommand command)
    {
        if (command.Title is not null && (string.IsNullOrWhiteSpace(command.Title) || command.Title.Length > 255)
            || command.Slug is not null && (command.Slug.Length > 300
                || !Regex.IsMatch(command.Slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant))
            || command.Description is not null && string.IsNullOrWhiteSpace(command.Description)
            || command.Price is <= 0 || command.DepositAmount is < 0 || command.MinLeaseMonths is <= 0)
            return ListingsErrors.InvalidInput;
        return ListingsValidation.ValidateCoordinates(command.Longitude, command.Latitude);
    }
}
