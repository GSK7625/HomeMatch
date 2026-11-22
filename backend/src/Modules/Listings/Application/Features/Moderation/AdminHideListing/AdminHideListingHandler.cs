using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.AdminHideListing;

public sealed record AdminHideListingCommand(long AdminId, long ListingId, string? Reason = null);

public sealed class AdminHideListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingResult>> HandleAsync(
        AdminHideListingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AdminId <= 0 || command.ListingId <= 0)
            return Result.Failure<ListingResult>(ListingsErrors.InvalidReview);

        if (command.Reason is not null && command.Reason.Trim().Length > 500)
            return Result.Failure<ListingResult>(ListingsErrors.InvalidReview);

        var listing = await listings.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing is null)
            return Result.Failure<ListingResult>(ListingsErrors.ListingNotFound);

        if (listing.Status is not ("PUBLISHED" or "PENDING_APPROVAL" or "HIDDEN"))
            return Result.Failure<ListingResult>(ListingsErrors.InvalidStatus);

        if (listing.Status != "HIDDEN")
        {
            var now = DateTimeOffset.UtcNow;
            listing.Status = "HIDDEN";
            listing.ReviewedBy = command.AdminId;
            listing.ReviewedAt = now;
            if (!string.IsNullOrWhiteSpace(command.Reason))
                listing.RejectionReason = command.Reason.Trim();
            listing.UpdatedAt = now;

            var saved = await listings.SaveChangesAsync(cancellationToken);
            if (saved.IsFailure) return Result.Failure<ListingResult>(saved.Error);
        }

        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
