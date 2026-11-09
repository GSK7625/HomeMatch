namespace HomeMatch.Modules.Viewings.Application.DTOs;

public record VirtualViewingDto(
    long Id,
    string RoomUrl,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset CreatedAt);

public record ViewingAppointmentDto(
    long Id,
    long TenantId,
    string TenantName,
    string? TenantPhone,
    long ListingId,
    string ListingTitle,
    decimal ListingPrice,
    string? ListingAddress,
    string? ListingPrimaryImageUrl,
    long LandlordId,
    string LandlordName,
    string? LandlordPhone,
    DateTimeOffset AppointmentTime,
    int DurationMinutes,
    string? Notes,
    string Status,
    string? CancelledBy,
    string? CancelledReason,
    DateTimeOffset? RemindAt,
    bool IsReminded,
    VirtualViewingDto? VirtualViewing,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record PagedViewingAppointmentsDto(
    IReadOnlyList<ViewingAppointmentDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
