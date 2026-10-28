using HomeMatch.Modules.Leasing.Domain.Enums;

namespace HomeMatch.Modules.Leasing.Application.DTOs;

public record LeadDto(
    long Id,
    long TenantId,
    long ListingId,
    long? RoomId,
    LeadStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record UpdateLeadRequest(
    LeadStatus Status,
    string? Note
);
