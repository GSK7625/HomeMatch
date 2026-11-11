namespace HomeMatch.Modules.Listings.Domain.Entities;

/// <summary>
/// Thực thể Khay so sánh phòng (UserRoomComparison - Tối đa 3 slot: 1, 2, 3)
/// </summary>
public sealed class UserRoomComparison
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ListingId { get; set; }
    public int Slot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Listing Listing { get; set; } = null!;
}

