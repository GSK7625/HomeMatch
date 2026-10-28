namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Danh mục Tiện ích (Amenity) như Điều hòa, Nóng lạnh, Máy giặt...
/// </summary>
public sealed class Amenity
{
    /// <summary>Mã định danh duy nhất của Tiện ích</summary>
    public long Id { get; set; }

    /// <summary>Tên hiển thị tiện ích (VD: "Điều hòa hai chiều", "Bình nóng lạnh", "Ban công thoáng mát")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã code định danh nội bộ (VD: 'AIR_CONDITIONER', 'WATER_HEATER', 'WASHING_MACHINE')</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Đường dẫn URL icon biểu tượng của tiện ích</summary>
    public string? IconUrl { get; set; }

    /// <summary>Phân loại tiện ích (FURNITURE: Nội thất, APPLIANCE: Thiết bị điện tử, FACILITY: Cơ sở vật chất tòa nhà)</summary>
    public string Category { get; set; } = string.Empty;
}
