namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record HandoverDetailsResponse(
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
