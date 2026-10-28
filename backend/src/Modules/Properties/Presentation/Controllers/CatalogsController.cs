using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Properties.Presentation.Controllers;

/// <summary>
/// Controller danh mục hành chính và tiện ích.
/// Tách biệt khỏi PropertiesController — tuân thủ SRP.
/// Các endpoint này là public (không yêu cầu đăng nhập).
/// </summary>
[ApiController]
[Tags("Catalogs")]
public sealed class CatalogsController(ICatalogReader catalogs) : ControllerBase
{
    /// <summary>Lấy danh sách tất cả tỉnh/thành phố</summary>
    [HttpGet("api/v1/administrative-provinces")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProvinceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvinces(CancellationToken cancellationToken)
    {
        var result = await catalogs.GetProvincesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProvinceResponse>>.Success(result));
    }

    /// <summary>Lấy danh sách phường/xã thuộc một tỉnh/thành phố</summary>
    [HttpGet("api/v1/administrative-provinces/{code}/wards")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WardResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWards(string code, CancellationToken cancellationToken)
    {
        var result = await catalogs.GetWardsAsync(code, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WardResponse>>.Success(result));
    }

    /// <summary>Lấy danh sách tất cả tiện ích (amenity)</summary>
    [HttpGet("api/v1/amenities")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AmenityResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAmenities(CancellationToken cancellationToken)
    {
        var result = await catalogs.GetAmenitiesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AmenityResponse>>.Success(result));
    }
}
