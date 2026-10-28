namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Biểu giá Dịch vụ & Điện Nước (UtilityTariff)
/// </summary>
public sealed class UtilityTariff
{
    /// <summary>Mã định danh duy nhất của Biểu giá</summary>
    public long Id { get; set; }

    /// <summary>ID của Tòa nhà áp dụng biểu giá này (khóa ngoại bảng properties)</summary>
    public long PropertyId { get; set; }

    /// <summary>Cột tương thích dữ liệu cũ; API MVP chỉ tạo biểu giá chung nên giá trị luôn NULL</summary>
    public long? RoomId { get; set; }

    /// <summary>Loại dịch vụ: ELECTRICITY, WATER, INTERNET, SERVICE hoặc CLEANING</summary>
    public string TariffType { get; set; } = string.Empty;

    /// <summary>Đơn vị giá: PER_KWH, PER_M3, PER_PERSON hoặc FIXED_ROOM</summary>
    public string CalcMethod { get; set; } = string.Empty;

    /// <summary>Đơn giá tính tiền (VNĐ/đơn vị tính)</summary>
    public long UnitPrice { get; set; }

    /// <summary>Ngày bắt đầu có hiệu lực của biểu giá</summary>
    public DateOnly EffectiveFrom { get; set; }
}
