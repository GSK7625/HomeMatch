using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

/// <summary>Request DTO để thêm ảnh / video / tour 360 vào tin đăng</summary>
public sealed class AddListingImageRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    [Url]
    public string ImageUrl { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    [RegularExpression("^(IMAGE|VIDEO|VIRTUAL_TOUR_360)$")]
    public string MediaType { get; init; } = "IMAGE";

    [StringLength(500)]
    [Url]
    public string? ThumbnailUrl { get; init; }

    [Range(1, int.MaxValue)]
    public int? DurationSeconds { get; init; }
}
