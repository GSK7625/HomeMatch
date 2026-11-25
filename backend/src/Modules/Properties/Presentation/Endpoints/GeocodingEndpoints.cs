using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Features.Geocoding;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

public static class GeocodingEndpoints
{
    public static void MapGeocodingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/geocoding", async ([AsParameters] GeocodingSearchRequest request, HttpContext context, GeocodingHandler handler, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var result = await handler.SearchAsync(request.AddressDetail, request.ProvinceCode, request.WardCode, cancellationToken);
            return result.IsFailure ? PropertiesHttpErrors.Failure(result.Error) : Results.Ok(ApiResponse.Success(result.Value));
        })
        .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
        .WithName("Properties.Geocoding").WithSummary("Tìm tọa độ từ địa chỉ và mã tỉnh/xã")
        .Produces<ApiResponse<IReadOnlyList<GeocodingResult>>>()
        .Produces<ApiResponse<object>>(400).Produces<ApiResponse<object>>(401).Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404).Produces<ApiResponse<object>>(500);

        group.MapGet("/reverse-geocoding", async ([AsParameters] ReverseGeocodingRequest request, HttpContext context, GeocodingHandler handler, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!decimal.TryParse(request.Latitude, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                || !decimal.TryParse(request.Longitude, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
                return PropertiesHttpErrors.Failure(Application.PropertiesErrors.InvalidCoordinates);
            var result = await handler.ReverseAsync(lat, lon, cancellationToken);
            return result.IsFailure ? PropertiesHttpErrors.Failure(result.Error) : Results.Ok(ApiResponse.Success(result.Value));
        })
        .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
        .WithName("Properties.ReverseGeocoding").WithSummary("Lấy địa chỉ gợi ý từ tọa độ, không thay đổi ghim")
        .Produces<ApiResponse<GeocodingResult>>()
        .Produces<ApiResponse<object>>(400).Produces<ApiResponse<object>>(401).Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404).Produces<ApiResponse<object>>(500);
    }
}
