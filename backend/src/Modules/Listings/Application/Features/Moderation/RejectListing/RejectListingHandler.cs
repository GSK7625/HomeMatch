using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Features.Moderation.RejectListing;

public sealed class RejectListingHandler(IListingRepository listings)
{
    public async Task<Result<ListingDto>> HandleAsync(RejectListingCommand command, CancellationToken cancellationToken = default)
    {
        if (command.AdminId <= 0 || string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
            return Result.Failure<ListingDto>(ListingsErrors.InvalidReview);
        var listing = await listings.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingDto>(ListingsErrors.ListingNotFound);
        if (listing.Status != "PENDING_APPROVAL") return Result.Failure<ListingDto>(ListingsErrors.InvalidStatus);
        listing.Status = "REJECTED";
        listing.ReviewedBy = command.AdminId;
        listing.ReviewedAt = DateTimeOffset.UtcNow;
        listing.RejectionReason = command.Reason.Trim();
        listing.UpdatedAt = listing.ReviewedAt.Value;
        var saved = await listings.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<ListingDto>(saved.Error);
        return Result.Success(ListingMapping.ToResult(listing, await listings.GetCoordinatesAsync(listing.Id, cancellationToken)));
    }
}
