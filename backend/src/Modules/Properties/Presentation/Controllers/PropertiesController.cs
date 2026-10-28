using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeMatch.Modules.Properties.Presentation.Controllers;

/// <summary>
/// Controller quản lý Bất động sản (Property).
/// Chỉ xử lý HTTP request/response — không chứa business logic.
/// </summary>
[ApiController]
[Route("api/v1/properties")]
[Authorize]
[Tags("Properties")]
public sealed class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok(new { module = "Properties", status = "Healthy" });

    /// <summary>Tạo mới một bất động sản</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PropertyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyService.CreateAsync(LandlordId(), request, cancellationToken);
        return Created($"/api/v1/properties/{result.Id}", ApiResponse<PropertyResponse>.Success(result));
    }

    /// <summary>Lấy danh sách tất cả bất động sản của chủ nhà đang đăng nhập</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PropertyResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await propertyService.GetMineAsync(LandlordId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PropertyResponse>>.Success(result));
    }

    /// <summary>Lấy chi tiết một bất động sản theo ID</summary>
    [HttpGet("{propertyId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long propertyId, CancellationToken cancellationToken)
    {
        var result = await propertyService.GetByIdAsync(LandlordId(), propertyId, cancellationToken);
        return Ok(ApiResponse<PropertyResponse>.Success(result));
    }

    /// <summary>Cập nhật thông tin bất động sản (PATCH — chỉ gửi field cần sửa)</summary>
    [HttpPatch("{propertyId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long propertyId, [FromBody] UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await propertyService.UpdateAsync(LandlordId(), propertyId, request, cancellationToken);
        return Ok(ApiResponse<PropertyResponse>.Success(result));
    }

    // ── Private helpers ────────────────────────────────────────────────────────
    private long LandlordId() =>
        RequestActor.LandlordId(User)
        ?? throw ApiException.Forbidden("Auth.LandlordRequired", "Cần tài khoản chủ nhà.");
}
