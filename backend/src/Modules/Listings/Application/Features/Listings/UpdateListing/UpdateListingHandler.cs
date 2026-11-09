using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;

public sealed class UpdateListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingResult>> HandleAsync(UpdateListingCommand command, CancellationToken cancellationToken = default)
    {
        var error = UpdateListingValidator.Validate(command);
        if (error is not null) return Result.Failure<ListingResult>(error);
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure<ListingResult>(ListingsErrors.EditLocked);
        var title = command.Title ?? listing.Title;
        var slug = command.Slug ?? listing.Slug;
        var description = command.Description ?? listing.Description;
        var price = command.Price ?? listing.Price;
        var deposit = command.DepositAmount ?? listing.DepositAmount;
        var leaseMonths = command.MinLeaseMonths ?? listing.MinLeaseMonths;
        var hasCurfew = command.HasCurfew ?? listing.HasCurfew;
        var curfewTime = command.CurfewTime ?? listing.CurfewTime;
        error = ListingsValidation.ValidateListing(title, slug, description, price, deposit, leaseMonths, hasCurfew, curfewTime);
        if (error is not null) return Result.Failure<ListingResult>(error);
        var cleanSlug = slug.Trim();
        if (cleanSlug != listing.Slug && await listings.SlugExistsAsync(cleanSlug, listing.Id, cancellationToken))
            return Result.Failure<ListingResult>(ListingsErrors.DuplicateSlug);
        listing.Title = title.Trim();
        listing.Slug = cleanSlug;
        listing.Description = description.Trim();
        listing.Price = price;
        listing.DepositAmount = deposit;
        listing.MinLeaseMonths = leaseMonths;
        listing.AllowPets = command.AllowPets ?? listing.AllowPets;
        listing.HasCurfew = hasCurfew;
        listing.CurfewTime = hasCurfew ? curfewTime : null;
        listing.IsFeatured = command.IsFeatured ?? listing.IsFeatured;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        if (command.Longitude.HasValue && command.Latitude.HasValue)
            await listings.SetCoordinatesAsync(listing, command.Longitude.Value, command.Latitude.Value, cancellationToken);
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
