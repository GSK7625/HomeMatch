namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Tỉnh / Thành phố trực thuộc Trung ương (AdministrativeProvince)
/// </summary>
public sealed class AdministrativeProvince
{
    /// <summary>Mã bưu chính / mã chuẩn của Tỉnh/Thành phố (Khóa chính, VD: '01' - Hà Nội, '79' - TP.HCM)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên ngắn gọn (VD: Hà Nội, Hồ Chí Minh, Đà Nẵng)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tên tiếng Anh (VD: Ha Noi, Ho Chi Minh)</summary>
    public string? NameEn { get; set; }

    /// <summary>Tên đầy đủ theo quy chuẩn nhà nước (VD: Thành phố Hà Nội, Tỉnh Bình Dương)</summary>
    public string FullName { get; set; } = string.Empty;
}
