namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;

public sealed record SearchListingsResponse(
    IReadOnlyList<ListingSummaryResponse> Items, int Page, int PageSize, int TotalCount);
