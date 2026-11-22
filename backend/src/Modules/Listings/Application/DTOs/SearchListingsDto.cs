namespace HomeMatch.Modules.Listings.Application.DTOs;

public sealed record SearchListingsDto(
    IReadOnlyList<ListingSummaryDto> Items, int Page, int PageSize, int TotalCount);
