using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;

public sealed record PropertyResponse(
    long Id,
    string Name,
    string ProvinceCode,
    string WardCode,
    string AddressDetail,
    string PropertyType,
    decimal? Latitude,
    decimal? Longitude)
{
    public static PropertyResponse From(PropertyResult value) =>
        new(
            value.Id,
            value.Name,
            value.ProvinceCode,
            value.WardCode,
            value.AddressDetail,
            value.PropertyType,
            value.Latitude,
            value.Longitude);
}
