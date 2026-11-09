namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;

public sealed record ListingReviewResponse(
    long Revision, long? ReviewedBy, DateTimeOffset? ReviewedAt,
    string? RejectionReason, DateTimeOffset? PublishedAt);

public sealed record PendingListingsResponse(
    IReadOnlyList<ListingSummaryResponse> Items, int Page, int PageSize, int TotalCount);
