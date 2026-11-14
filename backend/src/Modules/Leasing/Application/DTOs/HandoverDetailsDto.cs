namespace HomeMatch.Modules.Leasing.Application.DTOs;

public sealed record HandoverDetailsDto(
    long ContractId,
    string ContractCode,
    long RoomId,
    string ContractStatus,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset? HandoverDueAt,
    bool IsHandoverConfirmed,
    DateTimeOffset? HandoverConfirmedAt,
    long? HandoverConfirmedBy,
    DateOnly? ActualOccupancyEndDate,
    bool IsOccupancyReleased);
