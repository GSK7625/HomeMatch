using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;

public sealed class UpdateListingImageHandler(IListingRepository listings, IListingImageRepository images)
{
    public async Task<Result<ListingImageDto>> HandleAsync(UpdateListingImageCommand command, CancellationToken cancellationToken = default)
    {
        var error = UpdateListingImageValidator.Validate(command);
        if (error is not null) return Result.Failure<ListingImageDto>(error);
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingImageDto>(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure<ListingImageDto>(ListingsErrors.EditLocked);
        var image = listing.Images.FirstOrDefault(item => item.Id == command.ImageId);
        if (image is null) return Result.Failure<ListingImageDto>(ListingsErrors.ImageNotFound);
        if (command.IsPrimary == false && image.IsPrimary)
            return Result.Failure<ListingImageDto>(ListingsErrors.InvalidImage);
        image.IsPrimary = command.IsPrimary ?? image.IsPrimary;
        image.SortOrder = command.SortOrder ?? image.SortOrder;
        image.MediaType = command.MediaType ?? image.MediaType;
        image.ThumbnailUrl = command.ThumbnailUrl?.Trim() ?? image.ThumbnailUrl;
        image.DurationSeconds = command.DurationSeconds ?? image.DurationSeconds;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        listing.Revision++;
        var saved = await images.SaveAsync(image, false, cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingImageDto>(saved.Error);
        return Result.Success(ListingMapping.ToImageResult(image));
    }
}
