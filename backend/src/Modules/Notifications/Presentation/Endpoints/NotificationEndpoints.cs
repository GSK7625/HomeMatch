using System.Security.Claims;
using HomeMatch.Modules.Notifications.Application.Commands.MarkAllNotificationsRead;
using HomeMatch.Modules.Notifications.Application.Commands.MarkNotificationRead;
using HomeMatch.Modules.Notifications.Application.DTOs;
using HomeMatch.Modules.Notifications.Application.Queries.GetNotifications;
using HomeMatch.Modules.Notifications.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Notifications.Presentation.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("").RequireAuthorization();

        // 1. Lấy danh sách thông báo của tài khoản (GET /api/v1/notifications)
        group.MapGet("", async (
            [FromQuery] bool? isRead,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetNotificationsHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedNotificationsDto>(NotificationErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetNotificationsQuery(userId, isRead, page, pageSize),
                cancellationToken);

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetNotifications")
        .WithSummary("Xem danh sách thông báo của tài khoản (hỗ trợ lọc isRead và phân trang)")
        .Produces<ApiResponse<PagedNotificationsDto>>(StatusCodes.Status200OK);

        // 2. Đánh dấu đã đọc 1 thông báo (PATCH /api/v1/notifications/{id}/read)
        group.MapPatch("/{id:long}/read", async (
            long id,
            [FromServices] MarkNotificationReadHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<NotificationDto>(NotificationErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new MarkNotificationReadCommand(id, userId),
                cancellationToken);

            if (result.IsFailure)
            {
                var statusCode = result.Error.Code switch
                {
                    "Notification.NotFound" => StatusCodes.Status404NotFound,
                    "Notification.Forbidden" => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status400BadRequest
                };
                return Results.Json(ApiResponse.Failure<NotificationDto>(result.Error), statusCode: statusCode);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("MarkNotificationRead")
        .WithSummary("Đánh dấu một thông báo là đã đọc")
        .Produces<ApiResponse<NotificationDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<NotificationDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<NotificationDto>>(StatusCodes.Status404NotFound);

        // 3. Đánh dấu đã đọc tất cả thông báo (PATCH /api/v1/notifications/read-all)
        group.MapPatch("/read-all", async (
            [FromServices] MarkAllNotificationsReadHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<object>(NotificationErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new MarkAllNotificationsReadCommand(userId),
                cancellationToken);

            return Results.Ok(ApiResponse.Success(new { markedReadCount = result.Value }));
        })
        .WithName("MarkAllNotificationsRead")
        .WithSummary("Đánh dấu tất cả thông báo chưa đọc của tài khoản là đã đọc")
        .Produces(StatusCodes.Status200OK);

        return endpoints;
    }

    private static bool TryGetUserId(HttpContext httpContext, out long userId)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out userId);
    }
}
