using System.Security.Claims;
using HomeMatch.Modules.Listings.Application.Features.Reports;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints;

public static class ViolationReportsEndpoints
{
    public static void MapViolationReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/reports", CreateAsync)
            .WithTags("Violation Reports")
            .RequireAuthorization()
            .AddEndpointFilter<ListingRequestFilter>()
            .DisableValidation()
            .WithName("Reports.Create")
            .WithSummary("Người dùng gửi báo cáo vi phạm đối với tin đăng hoặc chủ nhà")
            .Produces<ApiResponse<ViolationReportResult>>(StatusCodes.Status201Created);

        var adminGroup = endpoints.MapGroup("/api/v1/admin/reports")
            .WithTags("Admin Reports")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"))
            .AddEndpointFilter<ListingRequestFilter>()
            .DisableValidation();

        adminGroup.MapGet("/", GetPagedAsync)
            .WithName("AdminReports.GetPaged")
            .WithSummary("Admin xem danh sách báo cáo vi phạm")
            .Produces<ApiResponse<PagedViolationReportsResult>>(StatusCodes.Status200OK);

        adminGroup.MapPost("/{reportId:long}/resolve", ResolveAsync)
            .WithName("AdminReports.Resolve")
            .WithSummary("Admin xác nhận vi phạm hoặc bác bỏ báo cáo")
            .Produces<ApiResponse<ViolationReportResult>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        [FromBody] CreateViolationReportRequest request,
        CreateViolationReportHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = RequestActor.UserId(user);
        if (userId is null) return ListingsHttpErrors.MissingUser();

        var result = await handler.HandleAsync(
            new CreateViolationReportCommand(
                userId.Value,
                request.TargetType,
                request.TargetId,
                request.ListingId,
                request.Reason,
                request.Description,
                request.EvidenceUrl),
            cancellationToken);

        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Created($"/api/v1/admin/reports/{result.Value.Id}", ApiResponse.Success(result.Value));
    }

    private static async Task<IResult> GetPagedAsync(
        ClaimsPrincipal user,
        [FromQuery] string? status,
        [FromQuery] string? targetType,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        GetViolationReportsHandler handler,
        CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return ListingsHttpErrors.MissingAdmin();
        var result = await handler.HandleAsync(
            new GetViolationReportsQuery(status, targetType, page ?? 1, pageSize ?? 20),
            cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse.Success(result.Value));
    }

    private static async Task<IResult> ResolveAsync(
        ClaimsPrincipal user,
        long reportId,
        [FromBody] ResolveViolationReportRequest request,
        ResolveViolationReportHandler handler,
        CancellationToken cancellationToken)
    {
        var adminId = RequestActor.AdminId(user);
        if (adminId is null) return ListingsHttpErrors.MissingAdmin();

        var result = await handler.HandleAsync(
            new ResolveViolationReportCommand(
                adminId.Value,
                reportId,
                request.Resolution,
                request.AdminNote,
                request.HideListing),
            cancellationToken);

        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse.Success(result.Value));
    }
}
