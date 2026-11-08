namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Danh mục Tiện ích (Amenity) như Điều hòa, Nóng lạnh, Máy giặt...
/// </summary>
public sealed class Amenity
{
    /// <summary>Mã định danh duy nhất của Tiện ích</summary>
    public long Id { get; set; }

    /// <summary>Tên hiển thị tiện ích (VD: "Điều hòa", "Bình nóng lạnh", "Ban công")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã code định danh nội bộ (VD: 'AIR_CONDITIONER', 'WATER_HEATER', 'WASHING_MACHINE')</summary>
    public string Code { get; set; } = string.Empty;
}
