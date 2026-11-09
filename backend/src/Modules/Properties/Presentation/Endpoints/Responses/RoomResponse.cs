using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record RoomResponse(
    long Id,
    long PropertyId,
    string RoomNumber,
    int Floor,
    decimal AreaSqm,
    string RoomType,
    int MaxOccupants,
    long BasePrice,
    string Status,
    DateOnly AvailableFrom,
    IReadOnlyList<long> AmenityIds)
{
    public static RoomResponse From(RoomResult value) =>
        new(
            value.Id,
            value.PropertyId,
            value.RoomNumber,
            value.Floor,
            value.AreaSqm,
            value.RoomType,
            value.MaxOccupants,
            value.BasePrice,
            value.Status,
            value.AvailableFrom,
            value.AmenityIds);
}
