using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

/// <summary>Request DTO để cập nhật ảnh tin đăng</summary>
public sealed class UpdateListingImageRequest
{
    public bool? IsPrimary { get; init; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; init; }

    [RegularExpression("^(IMAGE|VIDEO|VIRTUAL_TOUR_360)$")]
    public string? MediaType { get; init; }

    [StringLength(500)]
    [Url]
    public string? ThumbnailUrl { get; init; }

    [Range(1, int.MaxValue)]
    public int? DurationSeconds { get; init; }
}
