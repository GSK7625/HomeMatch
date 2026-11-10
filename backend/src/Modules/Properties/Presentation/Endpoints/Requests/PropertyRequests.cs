using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;

/// <summary>Request DTO để tạo mới bất động sản</summary>
public sealed record CreatePropertyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string ProvinceCode { get; init; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string WardCode { get; init; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string AddressDetail { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(APARTMENT|BOARDING_HOUSE|SERVICED_APARTMENT|HOMESTAY)$")]
    public string PropertyType { get; init; } = string.Empty;
}

/// <summary>Request DTO để cập nhật một phần bất động sản (PATCH)</summary>
public sealed record UpdatePropertyRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(20, MinimumLength = 1)]
    public string? ProvinceCode { get; init; }

    [StringLength(20, MinimumLength = 1)]
    public string? WardCode { get; init; }

    [StringLength(255, MinimumLength = 1)]
    public string? AddressDetail { get; init; }

    [RegularExpression("^(APARTMENT|BOARDING_HOUSE|SERVICED_APARTMENT|HOMESTAY)$")]
    public string? PropertyType { get; init; }
}
