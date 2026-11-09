using System.Security.Claims;
using HomeMatch.Modules.Viewings.Application.Commands.BookViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Commands.CancelViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Commands.CompleteViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Commands.ConfirmViewingAppointment;
using HomeMatch.Modules.Viewings.Application.DTOs;
using HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointmentById;
using HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointments;
using HomeMatch.Modules.Viewings.Domain.Constants;
using HomeMatch.Modules.Viewings.Presentation.Endpoints.Requests;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Viewings.Presentation.Endpoints;

public static class ViewingAppointmentEndpoints
{
    public static IEndpointRouteBuilder MapViewingAppointmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/appointments").RequireAuthorization();

        // 1. Đặt lịch xem phòng (POST /api/v1/viewings/appointments)
        group.MapPost("", async (
            [FromBody] BookViewingAppointmentRequest request,
            [FromServices] BookViewingAppointmentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ViewingAppointmentDto>(ViewingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ViewingAppointmentDto>(
                    Error.Validation("Viewing.ValidationFailed", "Dữ liệu đặt lịch xem phòng không hợp lệ.", validationErrors)));
            }

            var command = new BookViewingAppointmentCommand(
                TenantId: userId,
                ListingId: request.ListingId,
                AppointmentTime: request.AppointmentTime,
                DurationMinutes: request.DurationMinutes,
                Notes: request.Notes,
                IsVirtualViewing: request.IsVirtualViewing);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ViewingAppointmentDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/viewings/appointments/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .WithName("BookViewingAppointment")
        .WithSummary("Đặt lịch xem phòng mới (Dành cho Người thuê)")
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status409Conflict);

        // 2. Lấy danh sách lịch hẹn xem phòng (GET /api/v1/viewings/appointments)
        group.MapGet("", async (
            [FromQuery] string? status,
            [FromQuery] string? roleView,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetViewingAppointmentsHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedViewingAppointmentsDto>(ViewingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var effectiveRoleView = roleView;
            if (string.IsNullOrWhiteSpace(effectiveRoleView))
            {
                var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
                                ?? httpContext.User.FindFirst("role")?.Value;
                effectiveRoleView = string.Equals(roleClaim, "Landlord", StringComparison.OrdinalIgnoreCase)
                    ? "LANDLORD"
                    : "TENANT";
            }

            var query = new GetViewingAppointmentsQuery(
                UserId: userId,
                RoleView: effectiveRoleView,
                Status: status,
                Page: page,
                PageSize: pageSize);

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<PagedViewingAppointmentsDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetViewingAppointments")
        .WithSummary("Xem danh sách lịch hẹn xem phòng có phân trang (Người thuê hoặc Chủ nhà)")
        .Produces<ApiResponse<PagedViewingAppointmentsDto>>(StatusCodes.Status200OK);

        // 3. Xem chi tiết lịch hẹn (GET /api/v1/viewings/appointments/{id})
        group.MapGet("/{id:long}", async (
            long id,
            [FromServices] GetViewingAppointmentByIdHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ViewingAppointmentDto>(ViewingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetViewingAppointmentByIdQuery(id, userId),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<ViewingAppointmentDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetViewingAppointmentById")
        .WithSummary("Xem chi tiết một lịch hẹn xem phòng")
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status404NotFound);

        // 4. Chủ nhà xác nhận lịch xem phòng (PATCH /api/v1/viewings/appointments/{id}/confirm)
        group.MapPatch("/{id:long}/confirm", async (
            long id,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ConfirmViewingAppointmentRequest? request,
            [FromServices] ConfirmViewingAppointmentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ViewingAppointmentDto>(ViewingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (request is not null)
            {
                var validationErrors = request.Validate();
                if (validationErrors.Count > 0)
                {
                    return Results.BadRequest(ApiResponse.Failure<ViewingAppointmentDto>(
                        Error.Validation("Viewing.ValidationFailed", "Dữ liệu xác nhận lịch hẹn không hợp lệ.", validationErrors)));
                }
            }

            var result = await handler.HandleAsync(
                new ConfirmViewingAppointmentCommand(id, userId, request?.VirtualRoomUrl),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<ViewingAppointmentDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("ConfirmViewingAppointment")
        .WithSummary("Chủ nhà xác nhận lịch xem phòng (PENDING -> CONFIRMED)")
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status409Conflict);

        // 5. Hủy lịch xem phòng (PATCH /api/v1/viewings/appointments/{id}/cancel)
        group.MapPatch("/{id:long}/cancel", async (
            long id,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CancelViewingAppointmentRequest? request,
            [FromServices] CancelViewingAppointmentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ViewingAppointmentDto>(ViewingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (request is not null)
            {
                var validationErrors = request.Validate();
                if (validationErrors.Count > 0)
                {
                    return Results.BadRequest(ApiResponse.Failure<ViewingAppointmentDto>(
                        Error.Validation("Viewing.ValidationFailed", "Dữ liệu hủy lịch hẹn không hợp lệ.", validationErrors)));
                }
            }

            var result = await handler.HandleAsync(
                new CancelViewingAppointmentCommand(id, userId, request?.Reason),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<ViewingAppointmentDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("CancelViewingAppointment")
        .WithSummary("Hủy lịch xem phòng (Người thuê hoặc Chủ nhà)")
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status409Conflict);

        // 6. Đánh dấu hoàn thành buổi xem phòng (PATCH /api/v1/viewings/appointments/{id}/complete)
        group.MapPatch("/{id:long}/complete", async (
            long id,
            [FromServices] CompleteViewingAppointmentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ViewingAppointmentDto>(ViewingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new CompleteViewingAppointmentCommand(id, userId),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<ViewingAppointmentDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("CompleteViewingAppointment")
        .WithSummary("Đánh dấu hoàn thành buổi xem phòng (CONFIRMED -> COMPLETED)")
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ViewingAppointmentDto>>(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static bool TryGetUserId(HttpContext httpContext, out long userId)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out userId);
    }

    private static IResult MapFailureResult<T>(Error error)
    {
        var statusCode = error.Code switch
        {
            "Viewing.NotFound" or "Viewing.ListingNotFound" => StatusCodes.Status404NotFound,
            "Viewing.Forbidden" => StatusCodes.Status403Forbidden,
            "Viewing.ScheduleConflict" or "Viewing.InvalidStatusTransition" => StatusCodes.Status409Conflict,
            "Viewing.AuthenticationRequired" => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(ApiResponse.Failure<T>(error), statusCode: statusCode);
    }
}
