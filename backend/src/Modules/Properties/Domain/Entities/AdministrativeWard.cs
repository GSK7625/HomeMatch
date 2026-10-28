namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Xã / Phường trực thuộc Tỉnh/Thành phố (AdministrativeWard)
/// </summary>
public sealed class AdministrativeWard
{
    /// <summary>Mã định danh Xã/Phường (Khóa chính, VD: '00001')</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên ngắn gọn (VD: Dịch Vọng Hậu, Phúc Xá, Bến Nghé)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tên tiếng Anh (VD: Dich Vong Hau, Phuc Xa)</summary>
    public string? NameEn { get; set; }

    /// <summary>Tên đầy đủ theo quy chuẩn (VD: Phường Dịch Vọng Hậu, Xã Kim Chung)</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Tên Quận/Huyện cũ (phục vụ hiển thị ngữ cảnh đô thị, VD: Cầu Giấy, Ba Đình)</summary>
    public string? DistrictName { get; set; }

    /// <summary>Mã Tỉnh/Thành phố trực thuộc (Khóa ngoại liên kết bảng administrative_provinces)</summary>
    public string ProvinceCode { get; set; } = string.Empty;
}
