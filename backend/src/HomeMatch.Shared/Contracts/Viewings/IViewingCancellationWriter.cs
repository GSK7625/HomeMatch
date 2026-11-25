namespace HomeMatch.Shared.Contracts.Viewings;

public interface IViewingCancellationWriter
{
    Task CancelForListingAsync(long listingId, CancellationToken cancellationToken);
}
