namespace HomeMatch.Shared.Contracts.Listings;

public interface ITenantListingActivityReader
{
    Task<int> GetTotalViewedListingsCountAsync(long userId, CancellationToken cancellationToken = default);

    Task<int> GetTotalSavedListingsCountAsync(long userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ViewedListingContractItem> Items, int TotalCount)> GetViewedListingsPagedAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
