using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;

public static class AddListingImageValidator
{
    public static Error? Validate(AddListingImageCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ImageUrl) || command.ImageUrl.Length > 500
            || !Uri.TryCreate(command.ImageUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || command.MediaType is null)
            return ListingsErrors.InvalidImage;
        return ListingsValidation.ValidateMedia(command.MediaType, command.ThumbnailUrl, command.DurationSeconds, command.SortOrder);
    }
}
