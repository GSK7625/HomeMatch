using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetViewedListings;

public record GetViewedListingsQuery(
    long TenantId,
    int Page = 1,
    int PageSize = 20);

public record PagedViewedListingsDto(
    IReadOnlyList<ViewedListingContractItem> Items,
    int Page,
    int PageSize,
    int TotalCount);
