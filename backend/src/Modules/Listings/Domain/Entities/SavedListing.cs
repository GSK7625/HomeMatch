namespace HomeMatch.Modules.Listings.Domain.Entities;

/// <summary>
/// Thực thể Lưu tin đăng yêu thích của người dùng (SavedListing)
/// </summary>
public sealed class SavedListing
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ListingId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Listing Listing { get; set; } = null!;
}

