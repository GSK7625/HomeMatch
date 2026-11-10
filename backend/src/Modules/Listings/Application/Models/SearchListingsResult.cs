namespace HomeMatch.Modules.Listings.Application.Models;

public sealed record SearchListingsResult(
    IReadOnlyList<ListingSummaryResult> Items, int Page, int PageSize, int TotalCount);
