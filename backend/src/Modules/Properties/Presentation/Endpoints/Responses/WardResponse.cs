using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record WardResponse(
    string Code,
    string Name,
    string FullName,
    string ProvinceCode)
{
    public static WardResponse From(WardResult value) =>
        new(
            value.Code,
            value.Name,
            value.FullName,
            value.ProvinceCode);
}
