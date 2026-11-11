using HomeMatch.Modules.Listings.Application.Common;
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

        string slug;
        do
        {
            slug = SlugHelper.Generate(command.Title);
        } while (await listings.SlugExistsAsync(slug, null, cancellationToken));

        var now = DateTimeOffset.UtcNow;
        var listing = new Listing
        {
            RoomId = command.RoomId, LandlordId = command.LandlordId,
            Title = command.Title.Trim(), Slug = slug, Description = command.Description.Trim(),
            Price = command.Price, DepositAmount = command.DepositAmount, MinLeaseMonths = command.MinLeaseMonths,
            HouseRules = command.HouseRules?.Trim(),
            Status = "DRAFT", CreatedAt = now, UpdatedAt = now
        };
        listings.Add(listing);
        if (room.Longitude.HasValue && room.Latitude.HasValue)
            await listings.SetCoordinatesAsync(listing, (double)room.Longitude.Value, (double)room.Latitude.Value, cancellationToken);
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
