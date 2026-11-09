using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record ProvinceResponse(
    string Code,
    string Name,
    string FullName)
{
    public static ProvinceResponse From(ProvinceResult value) =>
        new(
            value.Code,
            value.Name,
            value.FullName);
}
