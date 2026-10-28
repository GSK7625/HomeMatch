namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Bất động sản / Tòa nhà cho thuê (Property)
/// </summary>
public sealed class Property
{
    /// <summary>Mã định danh duy nhất của Tòa nhà / Bất động sản</summary>
    public long Id { get; set; }

    /// <summary>ID của Chủ nhà sở hữu (tham chiếu tới bảng users trong module Identity)</summary>
    public long LandlordId { get; set; }

    /// <summary>Tên hiển thị của Tòa nhà (VD: "Chung cư mini Dịch Vọng", "Nhà trọ Hoa Mai")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã Tỉnh / Thành phố trực thuộc (VD: '01' - Hà Nội)</summary>
    public string ProvinceCode { get; set; } = string.Empty;

    /// <summary>Mã Phường / Xã trực thuộc</summary>
    public string WardCode { get; set; } = string.Empty;

    /// <summary>Địa chỉ chi tiết (Số nhà, tên ngõ, tên đường phố)</summary>
    public string AddressDetail { get; set; } = string.Empty;

    /// <summary>Loại hình bất động sản: BOARDING_HOUSE (Nhà trọ), APARTMENT (Căn hộ), MINI_APARTMENT (CCMN), WHOLE_HOUSE (Nhà nguyên căn)</summary>
    public string PropertyType { get; set; } = "BOARDING_HOUSE";

    /// <summary>Thời điểm tạo bản ghi</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Thời điểm cập nhật bản ghi gần nhất</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Danh sách các phòng trọ chi tiết trực thuộc tòa nhà này</summary>
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
