using System.Security.Claims;
using HomeMatch.Modules.Properties.Application.Features.AdminCatalogs;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

public static class AdminCatalogsEndpoints
{
    public static void MapAdminCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/catalogs")
            .WithTags("Admin Catalogs")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"))
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation();

        group.MapGet("/amenities", GetAmenitiesAsync)
            .WithName("AdminCatalogs.GetAmenities")
            .WithSummary("Admin lấy toàn bộ danh mục tiện ích (bao gồm đã ẩn)")
            .Produces<ApiResponse<IReadOnlyList<AmenityResponse>>>(StatusCodes.Status200OK);

        group.MapPost("/amenities", CreateAmenityAsync)
            .WithName("AdminCatalogs.CreateAmenity")
            .WithSummary("Admin thêm mới tiện ích")
            .Produces<ApiResponse<AmenityResponse>>(StatusCodes.Status201Created);

        group.MapPatch("/amenities/{id:long}", UpdateAmenityAsync)
            .WithName("AdminCatalogs.UpdateAmenity")
            .WithSummary("Admin chỉnh sửa hoặc ẩn/hiện tiện ích")
            .Produces<ApiResponse<AmenityResponse>>(StatusCodes.Status200OK);

        group.MapGet("/room-types", GetRoomTypesAsync)
            .WithName("AdminCatalogs.GetRoomTypes")
            .WithSummary("Admin lấy toàn bộ danh mục loại phòng (bao gồm đã ẩn)")
            .Produces<ApiResponse<IReadOnlyList<RoomTypeResponse>>>(StatusCodes.Status200OK);

        group.MapPost("/room-types", CreateRoomTypeAsync)
            .WithName("AdminCatalogs.CreateRoomType")
            .WithSummary("Admin thêm mới loại phòng")
            .Produces<ApiResponse<RoomTypeResponse>>(StatusCodes.Status201Created);

        group.MapPatch("/room-types/{id:long}", UpdateRoomTypeAsync)
            .WithName("AdminCatalogs.UpdateRoomType")
            .WithSummary("Admin chỉnh sửa hoặc ẩn/hiện loại phòng")
            .Produces<ApiResponse<RoomTypeResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAmenitiesAsync(
        ClaimsPrincipal user,
        GetAdminAmenitiesHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return MissingAdmin();
        var result = await handler.HandleAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success(result.Value.Select(AmenityResponse.From).ToArray()));
    }

    private static async Task<IResult> CreateAmenityAsync(
        ClaimsPrincipal user,
        [FromBody] CreateCatalogItemRequest request,
        CreateAmenityHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return MissingAdmin();
        var result = await handler.HandleAsync(new CreateAmenityCommand(request.Code, request.Name, request.IsActive), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var dto = AmenityResponse.From(result.Value);
        return Results.Created($"/api/v1/admin/catalogs/amenities/{dto.Id}", ApiResponse.Success(dto));
    }

    private static async Task<IResult> UpdateAmenityAsync(
        ClaimsPrincipal user,
        long id,
        [FromBody] UpdateCatalogItemRequest request,
        UpdateAmenityHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return MissingAdmin();
        var result = await handler.HandleAsync(new UpdateAmenityCommand(id, request.Code, request.Name, request.IsActive), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse.Success(AmenityResponse.From(result.Value)));
    }

    private static async Task<IResult> GetRoomTypesAsync(
        ClaimsPrincipal user,
        GetAdminRoomTypesHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return MissingAdmin();
        var result = await handler.HandleAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success(result.Value.Select(RoomTypeResponse.From).ToArray()));
    }

    private static async Task<IResult> CreateRoomTypeAsync(
        ClaimsPrincipal user,
        [FromBody] CreateCatalogItemRequest request,
        CreateRoomTypeHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return MissingAdmin();
        var result = await handler.HandleAsync(new CreateRoomTypeCommand(request.Code, request.Name, request.IsActive), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var dto = RoomTypeResponse.From(result.Value);
        return Results.Created($"/api/v1/admin/catalogs/room-types/{dto.Id}", ApiResponse.Success(dto));
    }

    private static async Task<IResult> UpdateRoomTypeAsync(
        ClaimsPrincipal user,
        long id,
        [FromBody] UpdateCatalogItemRequest request,
        UpdateRoomTypeHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return MissingAdmin();
        var result = await handler.HandleAsync(new UpdateRoomTypeCommand(id, request.Code, request.Name, request.IsActive), cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse.Success(RoomTypeResponse.From(result.Value)));
    }

    private static IResult MissingAdmin() =>
        PropertiesHttpErrors.Failure(new Error("Auth.AdminRequired", "Cần tài khoản quản trị viên hợp lệ."));
}
