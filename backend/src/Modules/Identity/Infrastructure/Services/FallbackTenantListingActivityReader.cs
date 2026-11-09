using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Bản cài đặt mặc định (Fallback) cho ITenantListingActivityReader giúp Mục 3.4 (Dashboard & Phòng đã xem)
/// hoạt động độc lập trong khi Module Listings (Mục 3.2) đang được phát triển song song.
/// Khi Module Listings đăng ký ITenantListingActivityReader chính thức, DI sẽ tự động ưu tiên bản chính thức.
/// </summary>
internal sealed class FallbackTenantListingActivityReader : ITenantListingActivityReader
{
    private static readonly IReadOnlyList<ViewedListingContractItem> EmptyItems = Array.Empty<ViewedListingContractItem>();

    public Task<int> GetTotalViewedListingsCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<int> GetTotalSavedListingsCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<(IReadOnlyList<ViewedListingContractItem> Items, int TotalCount)> GetViewedListingsPagedAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<(IReadOnlyList<ViewedListingContractItem>, int)>((EmptyItems, 0));
    }
}
