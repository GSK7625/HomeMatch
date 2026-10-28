using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý phòng trọ.
/// Tuân thủ SRP — tách biệt trách nhiệm quản lý Phòng khỏi quản lý Bất động sản.
/// Tuân thủ DIP — Controller phụ thuộc vào interface này, không phụ thuộc RoomService cụ thể.
/// </summary>
public interface IRoomService
{
    /// <summary>Tạo mới một phòng trong bất động sản</summary>
    Task<RoomResponse> CreateAsync(long landlordId, long propertyId, CreateRoomRequest request, CancellationToken cancellationToken);

    /// <summary>Lấy danh sách tất cả phòng trong một bất động sản</summary>
    Task<IReadOnlyList<RoomResponse>> GetByPropertyAsync(long landlordId, long propertyId, CancellationToken cancellationToken);

    /// <summary>Lấy thông tin chi tiết một phòng</summary>
    Task<RoomResponse> GetByIdAsync(long landlordId, long propertyId, long roomId, CancellationToken cancellationToken);

    /// <summary>Cập nhật thông tin phòng (PATCH)</summary>
    Task<RoomResponse> UpdateAsync(long landlordId, long propertyId, long roomId, UpdateRoomRequest request, CancellationToken cancellationToken);

    /// <summary>Thay đổi trạng thái phòng (AVAILABLE / RENTED / SUSPENDED)</summary>
    Task<RoomResponse> SetStatusAsync(long landlordId, long propertyId, long roomId, SetRoomStatusRequest request, CancellationToken cancellationToken);

    /// <summary>Ghi đè toàn bộ danh sách tiện ích của phòng (PUT)</summary>
    Task<RoomResponse> SetAmenitiesAsync(long landlordId, long propertyId, long roomId, SetRoomAmenitiesRequest request, CancellationToken cancellationToken);
}
