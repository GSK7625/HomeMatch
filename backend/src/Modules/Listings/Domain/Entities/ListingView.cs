namespace HomeMatch.Modules.Listings.Domain.Entities;

/// <summary>
/// Thực thể Nhật ký lượt xem tin đăng (ListingView)
/// </summary>
public sealed class ListingView
{
    public long Id { get; set; }
    public long ListingId { get; set; }
    public long? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset ViewedAt { get; set; }

    public Listing Listing { get; set; } = null!;
}

