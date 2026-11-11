using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;

public static class UpdateListingImageValidator
{
    public static Error? Validate(UpdateListingImageCommand command) =>
        ListingsValidation.ValidateMedia(command.MediaType, command.ThumbnailUrl, command.DurationSeconds, command.SortOrder);
}
