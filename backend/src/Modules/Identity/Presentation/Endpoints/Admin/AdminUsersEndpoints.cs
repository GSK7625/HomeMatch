using HomeMatch.Modules.Identity.Application.Features.Admin.GetAdminUsers;
using HomeMatch.Modules.Identity.Application.Features.Admin.SetUserLockStatus;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Admin;

public static class AdminUsersEndpoints
{
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/users")
            .WithTags("Admin Users")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));

        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] string? role,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetAdminUsersHandler handler = default!,
            CancellationToken cancellationToken = default) =>
        {
            var result = await handler.HandleAsync(
                new GetAdminUsersQuery(search, role, status, page, pageSize),
                cancellationToken);
            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("AdminUsers.GetList")
        .WithSummary("Admin xem và tìm kiếm danh sách tài khoản người thuê / chủ nhà")
        .Produces<ApiResponse<PagedAdminUsersResult>>(StatusCodes.Status200OK);

        group.MapPost("/{userId:long}/lock", async (
            long userId,
            [FromServices] SetUserLockStatusHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var adminId = RequestActor.AdminId(httpContext.User);
            if (adminId is null)
            {
                return Results.Json(
                    ApiResponse.Failure<AdminUserItemDto>(new Error("Auth.AdminRequired", "Cần quyền Quản trị viên.")),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await handler.HandleAsync(new SetUserLockStatusCommand(adminId.Value, userId, Lock: true), cancellationToken);
            return MapUserStatusResult(result);
        })
        .WithName("AdminUsers.Lock")
        .WithSummary("Admin khóa tài khoản người dùng")
        .Produces<ApiResponse<AdminUserItemDto>>(StatusCodes.Status200OK);

        group.MapPost("/{userId:long}/unlock", async (
            long userId,
            [FromServices] SetUserLockStatusHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var adminId = RequestActor.AdminId(httpContext.User);
            if (adminId is null)
            {
                return Results.Json(
                    ApiResponse.Failure<AdminUserItemDto>(new Error("Auth.AdminRequired", "Cần quyền Quản trị viên.")),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await handler.HandleAsync(new SetUserLockStatusCommand(adminId.Value, userId, Lock: false), cancellationToken);
            return MapUserStatusResult(result);
        })
        .WithName("AdminUsers.Unlock")
        .WithSummary("Admin mở khóa tài khoản người dùng")
        .Produces<ApiResponse<AdminUserItemDto>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static IResult MapUserStatusResult(Result<AdminUserItemDto> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(ApiResponse.Success(result.Value));
        }

        var statusCode = result.Error.Code switch
        {
            "Identity.UserNotFound" => StatusCodes.Status404NotFound,
            "Identity.CannotLockSelf" or "Identity.CannotLockAdmin" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(ApiResponse.Failure<AdminUserItemDto>(result.Error), statusCode: statusCode);
    }
}
