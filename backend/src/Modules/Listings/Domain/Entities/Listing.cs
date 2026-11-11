namespace HomeMatch.Modules.Listings.Domain.Entities;

/// <summary>
/// Thực thể Tin đăng cho thuê phòng trọ (Listing)
/// </summary>
public sealed class Listing
{
    /// <summary>Mã định danh duy nhất của Tin đăng</summary>
    public long Id { get; set; }

    /// <summary>ID phòng trọ đang được đăng tin (tham chiếu bảng rooms trong module Properties)</summary>
    public long RoomId { get; set; }

    /// <summary>ID chủ nhà tạo tin đăng (tham chiếu bảng users trong module Identity)</summary>
    public long LandlordId { get; set; }

    /// <summary>Tiêu đề bài viết tin đăng (VD: "Phòng trọ khép kín full đồ ban công thoáng ngõ 165 Cầu Giấy")</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Đường dẫn thân thiện SEO (Slug URL, VD: "phong-tro-khep-kin-cau-giay-a8f2c1")</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết phòng trọ, tiện ích, quy định giờ giấc</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Giá thuê niêm yết hàng tháng (VNĐ)</summary>
    public long Price { get; set; }

    /// <summary>Số tiền đặt cọc yêu cầu (VNĐ)</summary>
    public long DepositAmount { get; set; }

    /// <summary>Thời hạn hợp đồng tối thiểu theo tháng (Mặc định: 6 tháng)</summary>
    public int MinLeaseMonths { get; set; } = 6;

    /// <summary>Nội quy, quy định phòng trọ (VD: Giờ giấc tự do, cho nuôi thú cưng nhỏ, giữ yên tĩnh sau 23h)</summary>
    public string? HouseRules { get; set; }

    /// <summary>Trạng thái: DRAFT, PENDING_APPROVAL, REJECTED, PUBLISHED, HIDDEN, EXPIRED.</summary>
    public string Status { get; set; } = "DRAFT";

    public long Revision { get; set; } = 1;
    public long? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Tổng lượt xem tin đăng</summary>
    public long ViewCount { get; set; }

    /// <summary>Thời điểm công khai tin đăng.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Thời điểm tin đăng hết hạn hiển thị</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Thời điểm tạo tin đăng</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Thời điểm cập nhật tin gần nhất</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Danh sách album ảnh thực tế của căn phòng</summary>
    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
}
