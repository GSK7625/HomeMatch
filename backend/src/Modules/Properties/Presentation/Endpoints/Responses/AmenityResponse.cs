using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record AmenityResponse(
    long Id,
    string Code,
    string Name)
{
    public static AmenityResponse From(AmenityResult value) =>
        new(
            value.Id,
            value.Code,
            value.Name);
}
