namespace HomeMatch.Modules.Listings.Application.DTOs;

public sealed record ListingReviewDto(
    long Revision, long? ReviewedBy, DateTimeOffset? ReviewedAt,
    string? RejectionReason, DateTimeOffset? PublishedAt);

public sealed record PendingListingsDto(
    IReadOnlyList<ListingSummaryDto> Items, int Page, int PageSize, int TotalCount);
