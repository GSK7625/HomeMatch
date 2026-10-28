using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Application;

internal static class ListingResponseMapper
{
    public static async Task<ListingResponse> ToResponseAsync(
        ListingsDbContext db, Listing listing, CancellationToken cancellationToken)
    {
        var location = await db.ListingLocations.AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingId == listing.Id, cancellationToken);
        var images = listing.Images.Count > 0
            ? listing.Images.OrderBy(item => item.SortOrder).Select(ToImageResponse).ToArray()
            : await db.ListingImages.AsNoTracking().Where(item => item.ListingId == listing.Id)
                .OrderBy(item => item.SortOrder).Select(item => new ListingImageResponse(
                    item.Id, item.ImageUrl, item.IsPrimary, item.SortOrder))
                .ToArrayAsync(cancellationToken);
        return new ListingResponse(listing.Id, listing.RoomId, listing.LandlordId,
            listing.Title, listing.Slug, listing.Description, listing.Price,
            listing.DepositAmount, listing.MinLeaseMonths, listing.AllowPets,
            listing.HasCurfew, listing.CurfewTime, listing.Status,
            location?.Coordinates?.X, location?.Coordinates?.Y, images);
    }

    public static ListingSummaryResponse ToSummary(Listing listing) =>
        new(listing.Id, listing.RoomId, listing.Title, listing.Slug,
            listing.Price, listing.Status,
            listing.Images.FirstOrDefault(item => item.IsPrimary)?.ImageUrl);

    public static ListingImageResponse ToImageResponse(ListingImage image) =>
        new(image.Id, image.ImageUrl, image.IsPrimary, image.SortOrder);
}
