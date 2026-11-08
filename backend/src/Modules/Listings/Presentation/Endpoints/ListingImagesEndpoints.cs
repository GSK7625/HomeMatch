using System.Security.Claims;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.DeleteListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints;

public static class ListingImagesEndpoints
{
    public static void MapListingImageEndpoints(this RouteGroupBuilder group)
    {
        var imageGroup = group.MapGroup("/{listingId:long}/images")
            .RequireAuthorization()
            .WithTags("Listing Images");

        imageGroup.MapPost("/", AddImageAsync)
            .WithName("ListingImages.Add")
            .WithSummary("Thêm ảnh cho tin đăng")
            .Produces<ApiResponse<ListingImageResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        imageGroup.MapPatch("/{imageId:long}", UpdateImageAsync)
            .WithName("ListingImages.Update")
            .WithSummary("Cập nhật thông tin ảnh của tin đăng")
            .Produces<ApiResponse<ListingImageResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest);

        imageGroup.MapDelete("/{imageId:long}", DeleteImageAsync)
            .WithName("ListingImages.Delete")
            .WithSummary("Xóa ảnh của tin đăng")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> AddImageAsync(
        ClaimsPrincipal user,
        long listingId,
        [FromBody] AddListingImageRequest request,
        AddListingImageHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var command = new AddListingImageCommand(
            landlordId.Value,
            listingId,
            request.ImageUrl,
            request.IsPrimary,
            request.SortOrder,
            request.MediaType,
            request.ThumbnailUrl,
            request.DurationSeconds);

        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Created($"/api/v1/listings/{listingId}/images/{result.Value.Id}", ApiResponse<ListingImageResponse>.Success(ListingResponseMapper.ToResponse(result.Value)));
    }

    private static async Task<IResult> UpdateImageAsync(
        ClaimsPrincipal user,
        long listingId,
        long imageId,
        [FromBody] UpdateListingImageRequest request,
        UpdateListingImageHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var command = new UpdateListingImageCommand(
            landlordId.Value,
            listingId,
            imageId,
            request.IsPrimary,
            request.SortOrder,
            request.MediaType,
            request.ThumbnailUrl,
            request.DurationSeconds);

        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Ok(ApiResponse<ListingImageResponse>.Success(ListingResponseMapper.ToResponse(result.Value)));
    }

    private static async Task<IResult> DeleteImageAsync(
        ClaimsPrincipal user,
        long listingId,
        long imageId,
        DeleteListingImageHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();

        var command = new DeleteListingImageCommand(landlordId.Value, listingId, imageId);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.NoContent();
    }
}
