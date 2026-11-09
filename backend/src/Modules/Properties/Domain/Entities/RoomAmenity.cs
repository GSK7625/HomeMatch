namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Bảng liên kết Nhiều - Nhiều giữa Phòng trọ và Tiện ích (RoomAmenity)
/// </summary>
public sealed class RoomAmenity
{
    /// <summary>ID của Phòng trọ sở hữu tiện ích này (khóa ngoại bảng rooms)</summary>
    public long RoomId { get; set; }

    /// <summary>ID của Tiện ích được trang bị trong phòng (khóa ngoại bảng amenities)</summary>
    public long AmenityId { get; set; }
}
