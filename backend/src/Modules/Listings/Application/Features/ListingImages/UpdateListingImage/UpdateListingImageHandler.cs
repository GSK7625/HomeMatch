using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;

public sealed class UpdateListingImageHandler(IListingRepository listings, IListingImageRepository images)
{
    public async Task<Result<ListingImageResult>> HandleAsync(UpdateListingImageCommand command, CancellationToken cancellationToken = default)
    {
        var error = UpdateListingImageValidator.Validate(command);
        if (error is not null) return Result.Failure<ListingImageResult>(error);
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingImageResult>(ListingsErrors.ListingNotFound);
        var image = listing.Images.FirstOrDefault(item => item.Id == command.ImageId);
        if (image is null) return Result.Failure<ListingImageResult>(ListingsErrors.ImageNotFound);
        if (command.IsPrimary == false && image.IsPrimary)
            return Result.Failure<ListingImageResult>(ListingsErrors.InvalidImage);
        image.IsPrimary = command.IsPrimary ?? image.IsPrimary;
        image.SortOrder = command.SortOrder ?? image.SortOrder;
        image.MediaType = command.MediaType ?? image.MediaType;
        image.ThumbnailUrl = command.ThumbnailUrl?.Trim() ?? image.ThumbnailUrl;
        image.DurationSeconds = command.DurationSeconds ?? image.DurationSeconds;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        await images.SaveAsync(image, false, cancellationToken);
        return Result.Success(ListingMapping.ToImageResult(image));
    }
}
