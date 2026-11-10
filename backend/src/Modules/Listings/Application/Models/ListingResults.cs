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
    string? houseRules, string status,
    double? longitude, double? latitude, IReadOnlyList<ListingImageResult> images,
    long viewCount = 0, ListingReviewResult? review = null, DateTimeOffset? expiresAt = null)
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
    public string? HouseRules { get; } = houseRules;
    public string Status { get; } = status;
    public DateTimeOffset? ExpiresAt { get; } = expiresAt;
    public long ViewCount { get; } = viewCount;
    public ListingReviewResult? Review { get; } = review;
    public double? Longitude { get; } = longitude;
    public double? Latitude { get; } = latitude;
    public IReadOnlyList<ListingImageResult> Images { get; } = images;
}


/// <summary>Result DTO tóm tắt tin đăng (dùng cho danh sách và thẻ tìm kiếm)</summary>
public sealed class ListingSummaryResult(
    long id, long roomId, string title, string slug, long price,
    string status, string? coverImageUrl,
    long depositAmount = 0,
    decimal? areaSqm = null, string? roomType = null,
    string? districtName = null, string? provinceName = null, string? addressDetail = null,
    long? electricityPrice = null, long? waterPrice = null,
    IReadOnlyList<string>? amenities = null)
{
    public long Id { get; } = id;
    public long RoomId { get; } = roomId;
    public string Title { get; } = title;
    public string Slug { get; } = slug;
    public long Price { get; } = price;
    public long DepositAmount { get; } = depositAmount;
    public string Status { get; } = status;
    public string? CoverImageUrl { get; } = coverImageUrl;
    public decimal? AreaSqm { get; } = areaSqm;
    public string? RoomType { get; } = roomType;
    public string? DistrictName { get; } = districtName;
    public string? ProvinceName { get; } = provinceName;
    public string? AddressDetail { get; } = addressDetail;
    public long? ElectricityPrice { get; } = electricityPrice;
    public long? WaterPrice { get; } = waterPrice;
    public IReadOnlyList<string> Amenities { get; } = amenities ?? [];
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
