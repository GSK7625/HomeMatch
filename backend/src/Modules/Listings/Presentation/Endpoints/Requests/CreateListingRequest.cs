using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

/// <summary>Request DTO để tạo mới tin đăng</summary>
public sealed class CreateListingRequest : IValidatableObject
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long RoomId { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(300, MinimumLength = 1)]
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
    public bool IsFeatured { get; init; }

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
