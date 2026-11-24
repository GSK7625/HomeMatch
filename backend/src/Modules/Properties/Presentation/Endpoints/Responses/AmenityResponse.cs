using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record AmenityResponse(
    long Id,
    string Code,
    string Name,
    bool IsActive = true)
{
    public static AmenityResponse From(AmenityResult value) =>
        new(value.Id, value.Code, value.Name, value.IsActive);
}

public sealed record RoomTypeResponse(
    long Id,
    string Code,
    string Name,
    bool IsActive = true)
{
    public static RoomTypeResponse From(RoomTypeResult value) =>
        new(value.Id, value.Code, value.Name, value.IsActive);
}
