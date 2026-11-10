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
        var description = command.Description ?? listing.Description;
        var price = command.Price ?? listing.Price;
        var deposit = command.DepositAmount ?? listing.DepositAmount;
        var leaseMonths = command.MinLeaseMonths ?? listing.MinLeaseMonths;
        var houseRules = command.HouseRules is not null ? (string.IsNullOrWhiteSpace(command.HouseRules) ? null : command.HouseRules.Trim()) : listing.HouseRules;
        error = ListingsValidation.ValidateListing(title, description, price, deposit, leaseMonths, houseRules);
        if (error is not null) return Result.Failure<ListingResult>(error);
        listing.Title = title.Trim();
        listing.Description = description.Trim();
        listing.Price = price;
        listing.DepositAmount = deposit;
        listing.MinLeaseMonths = leaseMonths;
        listing.HouseRules = houseRules;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
