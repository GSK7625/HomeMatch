namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Phòng trọ chi tiết thuộc Tòa nhà (Room)
/// </summary>
public sealed class Room
{
    /// <summary>Mã định danh duy nhất của Phòng</summary>
    public long Id { get; set; }

    /// <summary>ID của Tòa nhà sở hữu căn phòng này (khóa ngoại liên kết bảng properties)</summary>
    public long PropertyId { get; set; }

    /// <summary>Số hiệu phòng (VD: "P.201", "P.302", "101")</summary>
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>Tầng của phòng (Mặc định: 1)</summary>
    public int Floor { get; set; } = 1;

    /// <summary>Diện tích phòng theo mét vuông (m2)</summary>
    public decimal AreaSqm { get; set; }

    /// <summary>Loại phòng: STUDIO, 1PN (1 phòng ngủ), 2PN (2 phòng ngủ), DUPLEX (gác xép), SHARED (ở ghép)</summary>
    public string RoomType { get; set; } = string.Empty;

    /// <summary>Số lượng người ở tối đa cho phép</summary>
    public int MaxOccupants { get; set; } = 2;

    /// <summary>Giá thuê cơ bản hàng tháng (VNĐ)</summary>
    public long BasePrice { get; set; }

    /// <summary>Trạng thái phòng: AVAILABLE (Đang trống), RENTED (Đã cho thuê), SUSPENDED (Tạm dừng cho thuê)</summary>
    public string Status { get; set; } = "AVAILABLE";

    /// <summary>Ngày bắt đầu có thể dọn vào ở</summary>
    public DateOnly AvailableFrom { get; set; }

    /// <summary>Thời điểm tạo bản ghi</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Thời điểm cập nhật bản ghi gần nhất</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Đối tượng Tòa nhà mà phòng này trực thuộc</summary>
    public Property Property { get; set; } = null!;
}
