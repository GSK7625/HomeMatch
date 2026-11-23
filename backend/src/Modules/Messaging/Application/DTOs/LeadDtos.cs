namespace HomeMatch.Modules.Messaging.Application.DTOs;

public sealed record LandlordLeadDto(
    long Id,
    long TenantId,
    string TenantName,
    string? TenantPhone,
    string? TenantEmail,
    long ListingId,
    string ListingTitle,
    long? RoomId,
    string? RoomNumber,
    long? PropertyId,
    string? PropertyName,
    string Status,
    string Source,
    string? Notes,
    long? ConversationId,
    DateTimeOffset CreatedAt);

public sealed record LandlordLeadFilterOptionDto(
    long ListingId,
    string ListingTitle,
    long? RoomId,
    string? RoomNumber,
    string? PropertyName);

public sealed record LandlordLeadStatusSummaryDto(
    int Total,
    int NewCount,
    int ContactedCount,
    int ViewingScheduledCount,
    int ContractSignedCount,
    int LostCount);

public sealed record LandlordLeadsResponseDto(
    IReadOnlyList<LandlordLeadDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    LandlordLeadStatusSummaryDto Summary,
    IReadOnlyList<LandlordLeadFilterOptionDto> ListingOptions);
