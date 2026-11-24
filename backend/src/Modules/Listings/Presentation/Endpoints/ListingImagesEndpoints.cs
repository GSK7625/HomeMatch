using System.Security.Claims;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.UploadListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.DeleteListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Features;

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

        imageGroup.MapPost("/uploads", UploadImageAsync)
            .WithName("ListingImages.Upload")
            .WithSummary("Tải ảnh hoặc video MP4 tin đăng lên Cloudflare R2")
            // Authentication uses a bearer token, not an ambient authentication cookie.
            .DisableAntiforgery()
            .Accepts<UploadListingImageRequest>("multipart/form-data")
            .Produces<ApiResponse<ListingImageResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

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

    private static async Task<IResult> UploadImageAsync(
        ClaimsPrincipal user, long listingId,
        HttpRequest request,
        UploadListingImageHandler handler, CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.LandlordId(user);
        if (landlordId is null) return ListingsHttpErrors.MissingLandlord();
        var limit = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false }) limit.MaxRequestBodySize = UploadListingImageHandler.MaxRequestBytes;
        if (!request.HasFormContentType) return ListingsHttpErrors.Failure(Application.ListingsErrors.InvalidImageFile);
        IFormCollection form;
        try { form = await request.ReadFormAsync(cancellationToken); }
        catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
        {
            return ListingsHttpErrors.Failure(Application.ListingsErrors.InvalidImageFile);
        }
        var file = form.Files.GetFile("file");
        if (file is null || form.Files.Count != 1
            || form["isPrimary"].Count > 1 || form["sortOrder"].Count > 1
            || !bool.TryParse(form["isPrimary"].FirstOrDefault() ?? "false", out var isPrimary)
            || !int.TryParse(form["sortOrder"].FirstOrDefault() ?? "0", out var sortOrder))
            return ListingsHttpErrors.Failure(Application.ListingsErrors.InvalidImageFile);
        await using var content = file.OpenReadStream();
        var result = await handler.HandleAsync(new(landlordId.Value, listingId, content,
            file.Length, file.FileName, file.ContentType, isPrimary, sortOrder), cancellationToken);
        if (result.IsFailure) return ListingsHttpErrors.Failure(result.Error);
        return Results.Created($"/api/v1/listings/{listingId}/images/{result.Value.Id}",
            ApiResponse<ListingImageResponse>.Success(ListingResponseMapper.ToResponse(result.Value)));
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
