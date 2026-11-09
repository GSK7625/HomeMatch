namespace HomeMatch.Modules.Listings.Application.Models;

public sealed record ListingReviewResult(
    long Revision, long? ReviewedBy, DateTimeOffset? ReviewedAt,
    string? RejectionReason, DateTimeOffset? PublishedAt);

public sealed record PendingListingsResult(
    IReadOnlyList<ListingSummaryResult> Items, int Page, int PageSize, int TotalCount);
