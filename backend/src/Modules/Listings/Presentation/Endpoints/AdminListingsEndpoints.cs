using System.Security.Claims;
using HomeMatch.Modules.Listings.Application.Features.Moderation.ApproveListing;
using HomeMatch.Modules.Listings.Application.Features.Moderation.RejectListing;
using HomeMatch.Modules.Listings.Application.Features.Moderation.GetPendingListings;
using HomeMatch.Modules.Listings.Application.Features.Moderation.GetReviewListing;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints;

public static class AdminListingsEndpoints
{
    public static void MapAdminListingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/listings")
            .WithTags("Admin Listings")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"))
            .AddEndpointFilter<ListingRequestFilter>()
            .DisableValidation();

        group.MapGet("/pending", GetPendingAsync).WithName("AdminListings.GetPending")
            .WithSummary("Admin lấy danh sách tin chờ duyệt")
            .Produces<ApiResponse<PendingListingsResponse>>(StatusCodes.Status200OK);
        group.MapGet("/{listingId:long}", GetByIdAsync).WithName("AdminListings.GetById")
            .WithSummary("Admin xem chi tiết tin và phiên bản cần duyệt")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK);
        group.MapPost("/{listingId:long}/approve", ApproveAsync).WithName("AdminListings.Approve")
            .WithSummary("Admin duyệt và công khai tin đăng")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK);
        group.MapPost("/{listingId:long}/reject", RejectAsync).WithName("AdminListings.Reject")
            .WithSummary("Admin từ chối tin đăng kèm lý do")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetPendingAsync(ClaimsPrincipal user, [FromQuery] int? page,
        [FromQuery] int? pageSize, GetPendingListingsHandler handler, CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return ListingsHttpErrors.MissingAdmin();
        var result = await handler.HandleAsync(new(page ?? 1, pageSize ?? 20), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        var value = result.Value;
        return Results.Ok(ApiResponse<PendingListingsResponse>.Success(new(
            ListingResponseMapper.ToResponse(value.Items), value.Page, value.PageSize, value.TotalCount)));
    }

    private static async Task<IResult> GetByIdAsync(ClaimsPrincipal user, long listingId,
        GetReviewListingHandler handler, CancellationToken cancellationToken)
    {
        if (RequestActor.AdminId(user) is null) return ListingsHttpErrors.MissingAdmin();
        var result = await handler.HandleAsync(new(listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> ApproveAsync(ClaimsPrincipal user, long listingId,
        ApproveListingHandler handler, CancellationToken cancellationToken)
    {
        var adminId = RequestActor.AdminId(user);
        if (adminId is null) return ListingsHttpErrors.MissingAdmin();
        var result = await handler.HandleAsync(new(adminId.Value, listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> RejectAsync(ClaimsPrincipal user, long listingId,
        [FromBody] RejectListingRequest request, RejectListingHandler handler, CancellationToken cancellationToken)
    {
        var adminId = RequestActor.AdminId(user);
        if (adminId is null) return ListingsHttpErrors.MissingAdmin();
        var result = await handler.HandleAsync(new(adminId.Value, listingId, request.Reason), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }
}
