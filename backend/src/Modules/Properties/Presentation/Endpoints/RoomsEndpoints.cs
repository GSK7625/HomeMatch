using System.Security.Claims;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;
using HomeMatch.Modules.Properties.Application.Features.Rooms.CreateRoom;
using HomeMatch.Modules.Properties.Application.Features.Rooms.GetPropertyRooms;
using HomeMatch.Modules.Properties.Application.Features.Rooms.GetRoomById;
using HomeMatch.Modules.Properties.Application.Features.Rooms.UpdateRoom;
using HomeMatch.Modules.Properties.Application.Features.Rooms.SetRoomStatus;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

public static class RoomsEndpoints
{
    public static void MapRoomEndpoints(this RouteGroupBuilder group)
    {
        var roomGroup = group.MapGroup("/{propertyId:long}/rooms")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .WithTags("Rooms");

        roomGroup.MapPost("/", CreateAsync)
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Rooms.Create")
            .WithSummary("Tạo mới phòng trong bất động sản")
            .Produces<ApiResponse<RoomResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        roomGroup.MapGet("/", GetByPropertyAsync)
            .WithName("Rooms.GetByProperty")
            .WithSummary("Lấy danh sách tất cả phòng trong bất động sản")
            .Produces<ApiResponse<IReadOnlyList<RoomResponse>>>(StatusCodes.Status200OK);

        roomGroup.MapGet("/{roomId:long}", GetByIdAsync)
            .WithName("Rooms.GetById")
            .WithSummary("Lấy chi tiết một phòng")
            .Produces<ApiResponse<RoomResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound);

        roomGroup.MapPatch("/{roomId:long}", UpdateAsync)
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Rooms.Update")
            .WithSummary("Cập nhật thông tin phòng")
            .Produces<ApiResponse<RoomResponse>>(StatusCodes.Status200OK);

        roomGroup.MapPatch("/{roomId:long}/status", SetStatusAsync)
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Rooms.SetStatus")
            .WithSummary("Thay đổi trạng thái phòng (AVAILABLE / RENTED / SUSPENDED)")
            .Produces<ApiResponse<RoomResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        long propertyId,
        [FromBody] CreateRoomRequest request,
        CreateRoomHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new CreateRoomCommand(
            landlordId.Value,
            propertyId,
            request.RoomNumber,
            request.Floor,
            request.AreaSqm,
            request.RoomType,
            request.MaxOccupants,
            request.AvailableFrom,
            request.AmenityIds);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = RoomResponse.From(result.Value);
        return Results.Created($"/api/v1/properties/{propertyId}/rooms/{result.Value.Id}", ApiResponse.Success(response));
    }

    private static async Task<IResult> GetByPropertyAsync(
        ClaimsPrincipal user,
        long propertyId,
        GetPropertyRoomsHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new GetPropertyRoomsQuery(
            landlordId.Value,
            propertyId);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = result.Value.Select(RoomResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> GetByIdAsync(
        ClaimsPrincipal user,
        long propertyId,
        long roomId,
        GetRoomByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new GetRoomByIdQuery(
            landlordId.Value,
            propertyId,
            roomId);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = RoomResponse.From(result.Value);
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> UpdateAsync(
        ClaimsPrincipal user,
        long propertyId,
        long roomId,
        [FromBody] UpdateRoomRequest request,
        UpdateRoomHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new UpdateRoomCommand(
            landlordId.Value,
            propertyId,
            roomId,
            request.RoomNumber,
            request.Floor,
            request.AreaSqm,
            request.RoomType,
            request.MaxOccupants,
            request.AvailableFrom,
            request.AmenityIds);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = RoomResponse.From(result.Value);
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> SetStatusAsync(
        ClaimsPrincipal user,
        long propertyId,
        long roomId,
        [FromBody] SetRoomStatusRequest request,
        SetRoomStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();
        var command = new SetRoomStatusCommand(
            landlordId.Value,
            propertyId,
            roomId,
            request.Status);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);
        var response = RoomResponse.From(result.Value);
        return Results.Ok(ApiResponse.Success(response));
    }
}
