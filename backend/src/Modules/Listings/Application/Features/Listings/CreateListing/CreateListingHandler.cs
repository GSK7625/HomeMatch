using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.CreateListing;

public sealed class CreateListingHandler(IListingRepository listings, IRoomListingReader rooms)
{
    public async Task<Result<ListingResult>> HandleAsync(CreateListingCommand command, CancellationToken cancellationToken = default)
    {
        var error = CreateListingValidator.Validate(command);
        if (error is not null) return Result.Failure<ListingResult>(error);
        var room = await rooms.GetRoomAsync(command.RoomId, cancellationToken);
        if (room is null || room.LandlordId != command.LandlordId)
            return Result.Failure<ListingResult>(ListingsErrors.RoomNotFound);
        var slug = command.Slug.Trim();
        if (await listings.SlugExistsAsync(slug, null, cancellationToken))
            return Result.Failure<ListingResult>(ListingsErrors.DuplicateSlug);
        var now = DateTimeOffset.UtcNow;
        var listing = new Listing
        {
            RoomId = command.RoomId, LandlordId = command.LandlordId,
            Title = command.Title.Trim(), Slug = slug, Description = command.Description.Trim(),
            Price = command.Price, DepositAmount = command.DepositAmount, MinLeaseMonths = command.MinLeaseMonths,
            AllowPets = command.AllowPets, HasCurfew = command.HasCurfew,
            CurfewTime = command.HasCurfew ? command.CurfewTime : null, IsFeatured = command.IsFeatured,
            Status = "DRAFT", CreatedAt = now, UpdatedAt = now
        };
        listings.Add(listing);
        if (command.Longitude.HasValue && command.Latitude.HasValue)
            await listings.SetCoordinatesAsync(listing, command.Longitude.Value, command.Latitude.Value, cancellationToken);
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
