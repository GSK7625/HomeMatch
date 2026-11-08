namespace HomeMatch.Modules.Listings.Application.Models;

/// <summary>Result DTO thông tin ảnh tin đăng</summary>
public sealed class ListingImageResult(
    long id, string imageUrl, bool isPrimary, int sortOrder,
    string mediaType = "IMAGE", string? thumbnailUrl = null, int? durationSeconds = null)
{
    public long Id { get; } = id;
    public string ImageUrl { get; } = imageUrl;
    public bool IsPrimary { get; } = isPrimary;
    public int SortOrder { get; } = sortOrder;
    public string MediaType { get; } = mediaType;
    public string? ThumbnailUrl { get; } = thumbnailUrl;
    public int? DurationSeconds { get; } = durationSeconds;
}


/// <summary>Result DTO thông tin đầy đủ tin đăng</summary>
public sealed class ListingResult(
    long id, long roomId, long landlordId, string title, string slug,
    string description, long price, long depositAmount, int minLeaseMonths,
    bool allowPets, bool hasCurfew, TimeOnly? curfewTime, string status,
    double? longitude, double? latitude, IReadOnlyList<ListingImageResult> images,
    bool isFeatured = false, long viewCount = 0)
{
    public long Id { get; } = id;
    public long RoomId { get; } = roomId;
    public long LandlordId { get; } = landlordId;
    public string Title { get; } = title;
    public string Slug { get; } = slug;
    public string Description { get; } = description;
    public long Price { get; } = price;
    public long DepositAmount { get; } = depositAmount;
    public int MinLeaseMonths { get; } = minLeaseMonths;
    public bool AllowPets { get; } = allowPets;
    public bool HasCurfew { get; } = hasCurfew;
    public TimeOnly? CurfewTime { get; } = curfewTime;
    public string Status { get; } = status;
    public bool IsFeatured { get; } = isFeatured;
    public long ViewCount { get; } = viewCount;
    public double? Longitude { get; } = longitude;
    public double? Latitude { get; } = latitude;
    public IReadOnlyList<ListingImageResult> Images { get; } = images;
}


/// <summary>Result DTO tóm tắt tin đăng (dùng cho danh sách)</summary>
public sealed class ListingSummaryResult(
    long id, long roomId, string title, string slug, long price,
    string status, string? coverImageUrl, bool isFeatured = false)
{
    public long Id { get; } = id;
    public long RoomId { get; } = roomId;
    public string Title { get; } = title;
    public string Slug { get; } = slug;
    public long Price { get; } = price;
    public string Status { get; } = status;
    public bool IsFeatured { get; } = isFeatured;
    public string? CoverImageUrl { get; } = coverImageUrl;
}


/// <summary>Result DTO tin đăng đã lưu yêu thích</summary>
public sealed class SavedListingResult(long id, long listingId, ListingSummaryResult listing, DateTimeOffset createdAt)
{
    public long Id { get; } = id;
    public long ListingId { get; } = listingId;
    public ListingSummaryResult Listing { get; } = listing;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}

public sealed record ListingCoordinates(double Longitude, double Latitude);
