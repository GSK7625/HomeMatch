using Microsoft.AspNetCore.Mvc;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;

public sealed class GeocodingSearchRequest
{
    [FromQuery(Name = "addressDetail")] public string? AddressDetail { get; init; }
    [FromQuery(Name = "provinceCode")] public string? ProvinceCode { get; init; }
    [FromQuery(Name = "wardCode")] public string? WardCode { get; init; }
}

public sealed class ReverseGeocodingRequest
{
    // Parse explicitly in the endpoint so malformed numbers also receive the standard error envelope.
    [FromQuery(Name = "latitude")] public string? Latitude { get; init; }
    [FromQuery(Name = "longitude")] public string? Longitude { get; init; }
}
