using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;

public sealed class UpdateListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingDto>> HandleAsync(UpdateListingCommand command, CancellationToken cancellationToken = default)
    {
        var error = UpdateListingValidator.Validate(command);
        if (error is not null) return Result.Failure<ListingDto>(error);
        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingDto>(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure<ListingDto>(ListingsErrors.EditLocked);
        var title = command.Title ?? listing.Title;
        var description = command.Description ?? listing.Description;
        var price = command.Price ?? listing.Price;
        var deposit = command.DepositAmount ?? listing.DepositAmount;
        var leaseMonths = command.MinLeaseMonths ?? listing.MinLeaseMonths;
        var houseRules = command.HouseRules is not null ? (string.IsNullOrWhiteSpace(command.HouseRules) ? null : command.HouseRules.Trim()) : listing.HouseRules;
        error = ListingsValidation.ValidateListing(title, description, price, deposit, leaseMonths, houseRules);
        if (error is not null) return Result.Failure<ListingDto>(error);
        listing.Title = title.Trim();
        listing.Description = description.Trim();
        listing.Price = price;
        listing.DepositAmount = deposit;
        listing.MinLeaseMonths = leaseMonths;
        listing.HouseRules = houseRules;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingDto>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
