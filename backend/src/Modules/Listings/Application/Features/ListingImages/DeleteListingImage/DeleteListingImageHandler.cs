using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.DeleteListingImage;

public sealed class DeleteListingImageHandler(IListingRepository listings, IListingImageRepository images)
{
    public async Task<Result> HandleAsync(DeleteListingImageCommand command, CancellationToken cancellationToken = default)
    {
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure(ListingsErrors.EditLocked);
        var image = listing.Images.FirstOrDefault(item => item.Id == command.ImageId);
        if (image is null) return Result.Failure(ListingsErrors.ImageNotFound);
        var replacement = image.IsPrimary
            ? listing.Images.Where(item => item.Id != image.Id).OrderBy(item => item.SortOrder).ThenBy(item => item.Id).FirstOrDefault()
            : null;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        listing.Revision++;
        return await images.DeleteAsync(image, replacement, cancellationToken);
    }
}
