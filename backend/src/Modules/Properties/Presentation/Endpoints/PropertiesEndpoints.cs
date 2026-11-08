using System.Security.Claims;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;
using HomeMatch.Modules.Properties.Application.Features.Properties.CreateProperty;
using HomeMatch.Modules.Properties.Application.Features.Properties.GetMyProperties;
using HomeMatch.Modules.Properties.Application.Features.Properties.GetPropertyById;
using HomeMatch.Modules.Properties.Application.Features.Properties.UpdateProperty;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

public static class PropertiesEndpoints
{
    public static void MapPropertyEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Properties.Create")
            .WithSummary("Tạo mới một bất động sản")
            .Produces<ApiResponse<PropertyResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        group.MapGet("/mine", GetMineAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Properties.GetMine")
            .WithSummary("Lấy danh sách tất cả bất động sản của chủ nhà đang đăng nhập")
            .Produces<ApiResponse<IReadOnlyList<PropertyResponse>>>(StatusCodes.Status200OK);

        group.MapGet("/{propertyId:long}", GetByIdAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Properties.GetById")
            .WithSummary("Lấy chi tiết một bất động sản theo ID")
            .Produces<ApiResponse<PropertyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound);

        group.MapPatch("/{propertyId:long}", UpdateAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Properties.Update")
            .WithSummary("Cập nhật thông tin bất động sản")
            .Produces<ApiResponse<PropertyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        [FromBody] CreatePropertyRequest request,
        CreatePropertyHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new CreatePropertyCommand(
            landlordId.Value,
            request.Name,
            request.ProvinceCode,
            request.WardCode,
            request.AddressDetail,
            request.PropertyType);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = PropertyResponse.From(result.Value);
        return Results.Created($"/api/v1/properties/{result.Value.Id}", ApiResponse.Success(response));
    }

    private static async Task<IResult> GetMineAsync(
        ClaimsPrincipal user,
        GetMyPropertiesHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new GetMyPropertiesQuery(
            landlordId.Value);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = result.Value.Select(PropertyResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> GetByIdAsync(
        ClaimsPrincipal user,
        long propertyId,
        GetPropertyByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new GetPropertyByIdQuery(
            landlordId.Value,
            propertyId);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = PropertyResponse.From(result.Value);
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> UpdateAsync(
        ClaimsPrincipal user,
        long propertyId,
        [FromBody] UpdatePropertyRequest request,
        UpdatePropertyHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new UpdatePropertyCommand(
            landlordId.Value,
            propertyId,
            request.Name,
            request.ProvinceCode,
            request.WardCode,
            request.AddressDetail,
            request.PropertyType);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = PropertyResponse.From(result.Value);
        return Results.Ok(ApiResponse.Success(response));
    }
}
