using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeMatch.Modules.Properties.Presentation.Controllers;

/// <summary>
/// Controller quản lý Đơn giá dịch vụ tiện ích (Tariff).
/// Quản lý biểu giá chung cấp Property.
/// </summary>
[ApiController]
[Authorize]
[Tags("Tariffs")]
public sealed class TariffsController(ITariffService tariffService) : ControllerBase
{
    /// <summary>Tạo đơn giá cấp bất động sản</summary>
    [HttpPost("api/v1/properties/{propertyId:long}/tariffs")]
    [ProducesResponseType(typeof(ApiResponse<TariffResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePropertyTariff(
        long propertyId, [FromBody] CreateTariffRequest request, CancellationToken cancellationToken)
    {
        var result = await tariffService.CreateAsync(LandlordId(), propertyId, request, cancellationToken);
        return Created($"/api/v1/properties/{propertyId}/tariffs/{result.Id}",
            ApiResponse<TariffResponse>.Success(result));
    }

    /// <summary>Lấy lịch sử đơn giá cấp bất động sản</summary>
    [HttpGet("api/v1/properties/{propertyId:long}/tariffs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TariffResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPropertyTariffHistory(
        long propertyId, CancellationToken cancellationToken)
    {
        var result = await tariffService.GetHistoryAsync(LandlordId(), propertyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TariffResponse>>.Success(result));
    }

    /// <summary>Cập nhật đơn giá cấp bất động sản (PATCH)</summary>
    [HttpPatch("api/v1/properties/{propertyId:long}/tariffs/{tariffId:long}")]
    [ProducesResponseType(typeof(ApiResponse<TariffResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePropertyTariff(
        long propertyId, long tariffId, [FromBody] UpdateTariffRequest request, CancellationToken cancellationToken)
    {
        var result = await tariffService.UpdateAsync(LandlordId(), propertyId, tariffId, request, cancellationToken);
        return Ok(ApiResponse<TariffResponse>.Success(result));
    }

    /// <summary>Lấy đơn giá chung đang hiệu lực tại ngày chỉ định</summary>
    [HttpGet("api/v1/properties/{propertyId:long}/tariffs/effective")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TariffResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffective(
        long propertyId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var result = await tariffService.GetEffectiveAsync(
            LandlordId(), propertyId, date ?? BusinessDate.Today(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TariffResponse>>.Success(result));
    }

    // ── Private helpers ────────────────────────────────────────────────────────
    private long LandlordId() =>
        RequestActor.LandlordId(User)
        ?? throw ApiException.Forbidden("Auth.LandlordRequired", "Cần tài khoản chủ nhà.");
}
