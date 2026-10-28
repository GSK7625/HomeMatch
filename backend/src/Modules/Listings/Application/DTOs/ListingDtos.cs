using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Application.DTOs;

/// <summary>Request DTO để tạo mới tin đăng</summary>
public sealed class CreateListingRequest : IValidatableObject
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long RoomId { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(long), "1", "9223372036854775807")]
    public long Price { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long DepositAmount { get; init; }

    [Range(1, int.MaxValue)]
    public int MinLeaseMonths { get; init; }

    public bool AllowPets { get; init; }
    public bool HasCurfew { get; init; }
    public TimeOnly? CurfewTime { get; init; }

    [Range(-180.0, 180.0)]
    public double? Longitude { get; init; }

    [Range(-90.0, 90.0)]
    public double? Latitude { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Longitude.HasValue != Latitude.HasValue)
            yield return new ValidationResult("Cần nhập cả kinh độ và vĩ độ.",
                [nameof(Longitude), nameof(Latitude)]);
        if (HasCurfew && CurfewTime is null)
            yield return new ValidationResult("Cần nhập giờ giới nghiêm.", [nameof(CurfewTime)]);
    }
}

/// <summary>Request DTO để cập nhật một phần tin đăng (PATCH)</summary>
public sealed class UpdateListingRequest : IValidatableObject
{
    [StringLength(255, MinimumLength = 1)]
    public string? Title { get; init; }

    [StringLength(255, MinimumLength = 1)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string? Slug { get; init; }

    public string? Description { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? Price { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long? DepositAmount { get; init; }

    [Range(1, int.MaxValue)]
    public int? MinLeaseMonths { get; init; }

    public bool? AllowPets { get; init; }
    public bool? HasCurfew { get; init; }
    public TimeOnly? CurfewTime { get; init; }

    [Range(-180.0, 180.0)]
    public double? Longitude { get; init; }

    [Range(-90.0, 90.0)]
    public double? Latitude { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Longitude.HasValue != Latitude.HasValue)
            yield return new ValidationResult("Cần nhập cả kinh độ và vĩ độ.",
                [nameof(Longitude), nameof(Latitude)]);
    }
}

/// <summary>Request DTO để thêm ảnh vào tin đăng</summary>
public sealed class AddListingImageRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    [Url]
    public string ImageUrl { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
}

/// <summary>Request DTO để cập nhật ảnh tin đăng</summary>
public sealed class UpdateListingImageRequest
{
    public bool? IsPrimary { get; init; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; init; }
}

/// <summary>Response DTO thông tin ảnh tin đăng</summary>
public sealed class ListingImageResponse(long id, string imageUrl, bool isPrimary, int sortOrder)
{
    public long Id { get; } = id;
    public string ImageUrl { get; } = imageUrl;
    public bool IsPrimary { get; } = isPrimary;
    public int SortOrder { get; } = sortOrder;
}

/// <summary>Response DTO thông tin đầy đủ tin đăng</summary>
public sealed class ListingResponse(
    long id, long roomId, long landlordId, string title, string slug,
    string description, long price, long depositAmount, int minLeaseMonths,
    bool allowPets, bool hasCurfew, TimeOnly? curfewTime, string status,
    double? longitude, double? latitude, IReadOnlyList<ListingImageResponse> images)
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
    public double? Longitude { get; } = longitude;
    public double? Latitude { get; } = latitude;
    public IReadOnlyList<ListingImageResponse> Images { get; } = images;
}

/// <summary>Response DTO tóm tắt tin đăng (dùng cho danh sách)</summary>
public sealed class ListingSummaryResponse(
    long id, long roomId, string title, string slug, long price,
    string status, string? coverImageUrl)
{
    public long Id { get; } = id;
    public long RoomId { get; } = roomId;
    public string Title { get; } = title;
    public string Slug { get; } = slug;
    public long Price { get; } = price;
    public string Status { get; } = status;
    public string? CoverImageUrl { get; } = coverImageUrl;
}
