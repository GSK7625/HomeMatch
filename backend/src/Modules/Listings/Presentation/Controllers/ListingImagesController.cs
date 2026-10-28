using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeMatch.Modules.Listings.Presentation.Controllers;

[ApiController]
[Route("api/v1/listings/{listingId:long}/images")]
[Authorize]
[Tags("Listing Images")]
public sealed class ListingImagesController(IListingImageService images) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add(long listingId,
        [FromBody] AddListingImageRequest request, CancellationToken cancellationToken)
    {
        var result = await images.AddImageAsync(LandlordId(), listingId, request,
            cancellationToken);
        return Created($"/api/v1/listings/{listingId}/images/{result.Id}",
            ApiResponse<ListingImageResponse>.Success(result));
    }

    [HttpPatch("{imageId:long}")]
    public async Task<IActionResult> Update(long listingId, long imageId,
        [FromBody] UpdateListingImageRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ListingImageResponse>.Success(
            await images.UpdateImageAsync(LandlordId(), listingId, imageId, request,
                cancellationToken)));

    [HttpDelete("{imageId:long}")]
    public async Task<IActionResult> Delete(long listingId, long imageId,
        CancellationToken cancellationToken)
    {
        await images.DeleteImageAsync(LandlordId(), listingId, imageId, cancellationToken);
        return NoContent();
    }

    private long LandlordId() => RequestActor.LandlordId(User)
        ?? throw ApiException.Forbidden("Auth.LandlordRequired", "Cần tài khoản chủ nhà.");
}
