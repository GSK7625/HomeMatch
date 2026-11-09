using System.Security.Claims;
using HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetTenantDashboard;
using HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetViewedListings;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Profile;

public static class TenantOverviewEndpoints
{
    public static IEndpointRouteBuilder MapTenantOverviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/tenant").RequireAuthorization().WithTags("Tenant Overview");

        // 1. Mục 3.4.1: Xem dashboard tìm phòng (GET /api/v1/identity/tenant/dashboard)
        group.MapGet("/dashboard", async (
            [FromServices] GetTenantDashboardHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<TenantDashboardSummaryDto>(
                        new Error("Identity.AuthenticationRequired", "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetTenantDashboardQuery(userId),
                cancellationToken);

            if (result.IsFailure)
            {
                return Results.NotFound(ApiResponse.Failure<TenantDashboardSummaryDto>(result.Error));
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetTenantDashboard")
        .WithSummary("Xem thống kê Dashboard tìm phòng của người thuê (số phòng đã xem, tin lưu, lịch hẹn, phiên chat)")
        .Produces<ApiResponse<TenantDashboardSummaryDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<TenantDashboardSummaryDto>>(StatusCodes.Status401Unauthorized);

        // 2. Mục 3.4.2: Xem danh sách phòng đã xem (GET /api/v1/identity/tenant/viewed-listings)
        group.MapGet("/viewed-listings", async (
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetViewedListingsHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedViewedListingsDto>(
                        new Error("Identity.AuthenticationRequired", "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetViewedListingsQuery(userId, page, pageSize),
                cancellationToken);

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetTenantViewedListings")
        .WithSummary("Xem lại danh sách các phòng đã truy cập (Lịch sử xem phòng của người thuê)")
        .Produces<ApiResponse<PagedViewedListingsDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedViewedListingsDto>>(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static bool TryGetUserId(HttpContext httpContext, out long userId)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out userId);
    }
}
