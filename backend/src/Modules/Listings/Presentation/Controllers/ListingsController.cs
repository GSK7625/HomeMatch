using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeMatch.Modules.Listings.Presentation.Controllers;

[ApiController]
[Route("api/v1/listings")]
[Authorize]
[Tags("Listings")]
public sealed class ListingsController(
    IListingWriteService writer, IListingReadService reader) : ControllerBase
{
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok(new { module = "Listings", status = "Healthy" });

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic([FromQuery] int? limit,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<ListingSummaryResponse>>.Success(
            await reader.GetPublicAsync(limit ?? 20, cancellationToken)));

    [HttpGet("{listingId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicById(long listingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<ListingResponse>.Success(
            await reader.GetPublicByIdAsync(listingId, cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await writer.CreateAsync(LandlordId(), request, cancellationToken);
        return Created($"/api/v1/listings/mine/{result.Id}",
            ApiResponse<ListingResponse>.Success(result));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<ListingSummaryResponse>>.Success(
            await reader.GetMineAsync(LandlordId(), cancellationToken)));

    [HttpGet("mine/{listingId:long}")]
    public async Task<IActionResult> GetMineById(long listingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<ListingResponse>.Success(
            await reader.GetMineByIdAsync(LandlordId(), listingId, cancellationToken)));

    [HttpPatch("{listingId:long}")]
    public async Task<IActionResult> Update(long listingId,
        [FromBody] UpdateListingRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ListingResponse>.Success(
            await writer.UpdateAsync(LandlordId(), listingId, request, cancellationToken)));

    [HttpPost("{listingId:long}/publish")]
    public async Task<IActionResult> Publish(long listingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<ListingResponse>.Success(
            await writer.PublishAsync(LandlordId(), listingId, cancellationToken)));

    [HttpPost("{listingId:long}/hide")]
    public async Task<IActionResult> Hide(long listingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<ListingResponse>.Success(
            await writer.HideAsync(LandlordId(), listingId, cancellationToken)));

    private long LandlordId() => RequestActor.LandlordId(User)
        ?? throw ApiException.Forbidden("Auth.LandlordRequired", "Cần tài khoản chủ nhà.");
}
