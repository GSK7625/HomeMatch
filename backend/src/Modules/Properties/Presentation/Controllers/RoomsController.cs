using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeMatch.Modules.Properties.Presentation.Controllers;

/// <summary>
/// Controller quản lý Phòng trọ (Room) trong một bất động sản.
/// Tách biệt khỏi PropertiesController — tuân thủ SRP.
/// </summary>
[ApiController]
[Route("api/v1/properties/{propertyId:long}/rooms")]
[Authorize]
[Tags("Rooms")]
public sealed class RoomsController(IRoomService roomService) : ControllerBase
{
    /// <summary>Tạo mới phòng trong bất động sản</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoomResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        long propertyId, [FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.CreateAsync(LandlordId(), propertyId, request, cancellationToken);
        return Created($"/api/v1/properties/{propertyId}/rooms/{result.Id}",
            ApiResponse<RoomResponse>.Success(result));
    }

    /// <summary>Lấy danh sách tất cả phòng trong bất động sản</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoomResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProperty(long propertyId, CancellationToken cancellationToken)
    {
        var result = await roomService.GetByPropertyAsync(LandlordId(), propertyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoomResponse>>.Success(result));
    }

    /// <summary>Lấy chi tiết một phòng</summary>
    [HttpGet("{roomId:long}")]
    [ProducesResponseType(typeof(ApiResponse<RoomResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        long propertyId, long roomId, CancellationToken cancellationToken)
    {
        var result = await roomService.GetByIdAsync(LandlordId(), propertyId, roomId, cancellationToken);
        return Ok(ApiResponse<RoomResponse>.Success(result));
    }

    /// <summary>Cập nhật thông tin phòng (PATCH)</summary>
    [HttpPatch("{roomId:long}")]
    [ProducesResponseType(typeof(ApiResponse<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        long propertyId, long roomId, [FromBody] UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.UpdateAsync(LandlordId(), propertyId, roomId, request, cancellationToken);
        return Ok(ApiResponse<RoomResponse>.Success(result));
    }

    /// <summary>Thay đổi trạng thái phòng (AVAILABLE / RENTED / SUSPENDED)</summary>
    [HttpPatch("{roomId:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetStatus(
        long propertyId, long roomId, [FromBody] SetRoomStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.SetStatusAsync(LandlordId(), propertyId, roomId, request, cancellationToken);
        return Ok(ApiResponse<RoomResponse>.Success(result));
    }

    /// <summary>Ghi đè toàn bộ danh sách tiện ích của phòng (PUT)</summary>
    [HttpPut("{roomId:long}/amenities")]
    [ProducesResponseType(typeof(ApiResponse<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAmenities(
        long propertyId, long roomId, [FromBody] SetRoomAmenitiesRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.SetAmenitiesAsync(LandlordId(), propertyId, roomId, request, cancellationToken);
        return Ok(ApiResponse<RoomResponse>.Success(result));
    }

    // ── Private helpers ────────────────────────────────────────────────────────
    private long LandlordId() =>
        RequestActor.LandlordId(User)
        ?? throw ApiException.Forbidden("Auth.LandlordRequired", "Cần tài khoản chủ nhà.");
}
