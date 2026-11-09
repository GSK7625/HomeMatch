using System.Security.Claims;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;
using HomeMatch.Modules.Listings.Application.Features.Listings.CreateListing;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetMyListingById;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetMyListings;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListings;
using HomeMatch.Modules.Listings.Application.Features.Listings.HideListing;
using HomeMatch.Modules.Listings.Application.Features.Listings.SubmitListing;
using HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;
using HomeMatch.Modules.Listings.Application.Features.SavedListings.GetSavedListings;
using HomeMatch.Modules.Listings.Application.Features.SavedListings.ToggleSaveListing;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints;

public static class ListingsEndpoints
{
    public static void MapListingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetPublicAsync)
            .AllowAnonymous()
            .WithName("Listings.GetPublic")
            .WithSummary("Lấy danh sách tin đăng công khai")
            .Produces<ApiResponse<IReadOnlyList<ListingSummaryResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{listingId:long}", GetPublicByIdAsync)
            .AllowAnonymous()
            .WithName("Listings.GetPublicById")
            .WithSummary("Lấy chi tiết tin đăng công khai")
            .Produces<ApiResponse<PublicListingDetailResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization()
            .WithName("Listings.Create")
            .WithSummary("Tạo mới tin đăng")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        group.MapGet("/mine", GetMineAsync)
            .RequireAuthorization()
            .WithName("Listings.GetMine")
            .WithSummary("Lấy danh sách tin đăng của chủ nhà")
            .Produces<ApiResponse<IReadOnlyList<ListingSummaryResponse>>>(StatusCodes.Status200OK);

        group.MapGet("/mine/{listingId:long}", GetMineByIdAsync)
            .RequireAuthorization()
            .WithName("Listings.GetMineById")
            .WithSummary("Lấy chi tiết tin đăng của chủ nhà")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound);

        group.MapPatch("/{listingId:long}", UpdateAsync)
            .RequireAuthorization()
            .WithName("Listings.Update")
            .WithSummary("Cập nhật thông tin tin đăng")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        group.MapPost("/{listingId:long}/submit", SubmitAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .WithName("Listings.Submit")
            .WithSummary("Chủ nhà gửi tin đăng để Admin duyệt")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        group.MapPost("/{listingId:long}/publish", SubmitAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .WithMetadata(new ObsoleteAttribute("Dùng POST /api/v1/listings/{listingId}/submit."))
            .WithName("Listings.Publish")
            .WithSummary("[Deprecated] Gửi duyệt tin; dùng /submit. Không tự công khai tin.")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        group.MapPost("/{listingId:long}/hide", HideAsync)
            .RequireAuthorization()
            .WithName("Listings.Hide")
            .WithSummary("Ẩn tin đăng")
            .Produces<ApiResponse<ListingResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        // ── Saved Listings (Favorites) ──────────────────────────────────────────
        group.MapPost("/{listingId:long}/save", ToggleSaveAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Tenant"))
            .WithName("Listings.ToggleSave")
            .WithSummary("Người thuê lưu hoặc bỏ lưu tin đăng yêu thích")
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/saved", GetSavedAsync)
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Tenant"))
            .WithName("Listings.GetSaved")
            .WithSummary("Lấy danh sách tin đăng yêu thích của người thuê")
            .Produces<ApiResponse<IReadOnlyList<SavedListingResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

    }

    private static async Task<IResult> GetPublicAsync(
        [FromQuery] int? limit,
        GetPublicListingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPublicListingsQuery(limit ?? 20), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<IReadOnlyList<ListingSummaryResponse>>.Success(ListingResponseMapper.ToResponse(result.Value)));
    }

    private static async Task<IResult> GetPublicByIdAsync(
        long listingId,
        GetPublicListingByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPublicListingByIdQuery(listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<PublicListingDetailResponse>.Success(ListingResponseMapper.ToResponse(result.Value)));
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        [FromBody] CreateListingRequest request,
        CreateListingHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var command = new CreateListingCommand(
            landlordId.Value,
            request.RoomId,
            request.Title,
            request.Slug,
            request.Description,
            request.Price,
            request.DepositAmount,
            request.MinLeaseMonths,
            request.AllowPets,
            request.HasCurfew,
            request.CurfewTime,
            request.IsFeatured,
            request.Longitude,
            request.Latitude);

        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Created($"/api/v1/listings/mine/{result.Value.Id}", ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> GetMineAsync(
        ClaimsPrincipal user,
        GetMyListingsHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var result = await handler.HandleAsync(new GetMyListingsQuery(landlordId.Value), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<IReadOnlyList<ListingSummaryResponse>>.Success(ListingResponseMapper.ToResponse(result.Value)));
    }

    private static async Task<IResult> GetMineByIdAsync(
        ClaimsPrincipal user,
        long listingId,
        GetMyListingByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var result = await handler.HandleAsync(new GetMyListingByIdQuery(landlordId.Value, listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> UpdateAsync(
        ClaimsPrincipal user,
        long listingId,
        [FromBody] UpdateListingRequest request,
        UpdateListingHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var command = new UpdateListingCommand(
            landlordId.Value,
            listingId,
            request.Title,
            request.Slug,
            request.Description,
            request.Price,
            request.DepositAmount,
            request.MinLeaseMonths,
            request.AllowPets,
            request.HasCurfew,
            request.CurfewTime,
            request.IsFeatured,
            request.Longitude,
            request.Latitude);

        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> SubmitAsync(
        ClaimsPrincipal user,
        long listingId,
        SubmitListingHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var result = await handler.HandleAsync(new SubmitListingCommand(landlordId.Value, listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> HideAsync(
        ClaimsPrincipal user,
        long listingId,
        HideListingHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var result = await handler.HandleAsync(new HideListingCommand(landlordId.Value, listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingResponse>.Success(ListingResponseMapper.ToResponse(result.Value, includeReview: true)));
    }

    private static async Task<IResult> ToggleSaveAsync(
        ClaimsPrincipal user,
        long listingId,
        ToggleSaveListingHandler handler,
        CancellationToken cancellationToken)
    {
        var tenantId = RequestActor.TenantId(user);
        if (tenantId is null) return ListingsHttpErrors.MissingTenant();

        var result = await handler.HandleAsync(new ToggleSaveListingCommand(tenantId.Value, listingId), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<bool>.Success(result.Value));
    }

    private static async Task<IResult> GetSavedAsync(
        ClaimsPrincipal user,
        GetSavedListingsHandler handler,
        CancellationToken cancellationToken)
    {
        var tenantId = RequestActor.TenantId(user);
        if (tenantId is null) return ListingsHttpErrors.MissingTenant();

        var result = await handler.HandleAsync(new GetSavedListingsQuery(tenantId.Value), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<IReadOnlyList<SavedListingResponse>>.Success(ListingResponseMapper.ToResponse(result.Value)));
    }
}
