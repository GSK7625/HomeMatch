namespace HomeMatch.Modules.Listings.Domain.Entities;

/// <summary>
/// Thực thể Hình ảnh bài đăng phòng trọ (ListingImage)
/// </summary>
public sealed class ListingImage
{
    /// <summary>Mã định danh duy nhất của hình ảnh</summary>
    public long Id { get; set; }

    /// <summary>ID tin đăng sở hữu hình ảnh này (khóa ngoại bảng listings)</summary>
    public long ListingId { get; set; }

    /// <summary>Đường dẫn URL ảnh lưu trữ trên Cloudflare R2 / CDN</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Cờ đánh dấu có phải ảnh đại diện (ảnh bìa) của tin hay không</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Thứ tự sắp xếp hiển thị ảnh trong album (0, 1, 2...)</summary>
    public int SortOrder { get; set; }

    /// <summary>Thời điểm tải ảnh lên</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Đối tượng Tin đăng mà ảnh này trực thuộc</summary>
    public Listing Listing { get; set; } = null!;
}
