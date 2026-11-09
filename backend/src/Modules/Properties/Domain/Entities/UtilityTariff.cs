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

    /// <summary>Áp dụng riêng cho phòng (nếu NULL nghĩa là áp dụng chung cho cả tòa nhà)</summary>
    public long? RoomId { get; set; }

    /// <summary>Loại dịch vụ: ELECTRICITY, WATER, INTERNET, SERVICE, CLEANING, PARKING</summary>
    public string TariffType { get; set; } = string.Empty;

    /// <summary>Cách tính: PER_KWH, PER_M3, PER_PERSON, FIXED_PER_MONTH, FREE</summary>
    public string CalcMethod { get; set; } = string.Empty;

    /// <summary>Đơn giá tính tiền (VNĐ/đơn vị tính)</summary>
    public long UnitPrice { get; set; }

    /// <summary>Thời điểm tạo bản ghi</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
