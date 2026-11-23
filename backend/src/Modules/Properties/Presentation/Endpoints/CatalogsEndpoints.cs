using HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;
using HomeMatch.Modules.Properties.Application.Features.Catalogs.GetProvinces;
using HomeMatch.Modules.Properties.Application.Features.Catalogs.GetWards;
using HomeMatch.Modules.Properties.Application.Features.Catalogs.GetAmenities;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

public static class CatalogsEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .WithTags("Catalogs")
            .AllowAnonymous();

        group.MapGet("/administrative-provinces", GetProvincesAsync)
            .WithName("Catalogs.GetProvinces")
            .WithSummary("Lấy danh sách tất cả tỉnh/thành phố")
            .Produces<ApiResponse<IReadOnlyList<ProvinceResponse>>>(StatusCodes.Status200OK);

        group.MapGet("/administrative-provinces/{code}/wards", GetWardsAsync)
            .WithName("Catalogs.GetWards")
            .WithSummary("Lấy danh sách phường/xã thuộc một tỉnh/thành phố")
            .Produces<ApiResponse<IReadOnlyList<WardResponse>>>(StatusCodes.Status200OK);

        group.MapGet("/amenities", GetAmenitiesAsync)
            .WithName("Catalogs.GetAmenities")
            .WithSummary("Lấy danh sách tất cả tiện ích (amenity)")
            .Produces<ApiResponse<IReadOnlyList<AmenityResponse>>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetProvincesAsync(
        GetProvincesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetProvincesQuery(), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = result.Value.Select(ProvinceResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> GetWardsAsync(
        string code,
        GetWardsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWardsQuery(code), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = result.Value.Select(WardResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> GetAmenitiesAsync(
        GetAmenitiesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAmenitiesQuery(), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = result.Value.Select(AmenityResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }
}
