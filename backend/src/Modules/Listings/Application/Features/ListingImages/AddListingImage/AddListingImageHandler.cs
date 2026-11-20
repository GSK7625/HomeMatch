using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;

public sealed class AddListingImageHandler(IListingRepository listings, IListingImageRepository images)
{
    public async Task<Result<ListingImageDto>> HandleAsync(AddListingImageCommand command, CancellationToken cancellationToken = default)
    {
        var error = AddListingImageValidator.Validate(command);
        if (error is not null) return Result.Failure<ListingImageDto>(error);
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingImageDto>(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure<ListingImageDto>(ListingsErrors.EditLocked);
        var image = new ListingImage
        {
            ListingId = listing.Id, ImageUrl = command.ImageUrl.Trim(),
            IsPrimary = command.IsPrimary || listing.Images.Count == 0, SortOrder = command.SortOrder,
            MediaType = command.MediaType, ThumbnailUrl = command.ThumbnailUrl?.Trim(),
            DurationSeconds = command.DurationSeconds, CreatedAt = DateTimeOffset.UtcNow
        };
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        listing.Revision++;
        var saved = await images.SaveAsync(image, true, cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingImageDto>(saved.Error);
        return Result.Success(ListingMapping.ToImageResult(image));
    }
}
