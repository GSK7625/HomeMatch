using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Application.DTOs;

/// <summary>Request DTO để tạo mới bất động sản</summary>
public sealed class CreatePropertyRequest
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
    [RegularExpression("^(BOARDING_HOUSE|APARTMENT|MINI_APARTMENT|WHOLE_HOUSE)$")]
    public string PropertyType { get; init; } = string.Empty;
}

/// <summary>Request DTO để cập nhật một phần bất động sản (PATCH)</summary>
public sealed class UpdatePropertyRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(20, MinimumLength = 1)]
    public string? ProvinceCode { get; init; }

    [StringLength(20, MinimumLength = 1)]
    public string? WardCode { get; init; }

    [StringLength(255, MinimumLength = 1)]
    public string? AddressDetail { get; init; }

    [RegularExpression("^(BOARDING_HOUSE|APARTMENT|MINI_APARTMENT|WHOLE_HOUSE)$")]
    public string? PropertyType { get; init; }
}

/// <summary>Response DTO trả về thông tin bất động sản</summary>
public sealed class PropertyResponse(
    long id, string name, string provinceCode, string wardCode,
    string addressDetail, string propertyType)
{
    public long Id { get; } = id;
    public string Name { get; } = name;
    public string ProvinceCode { get; } = provinceCode;
    public string WardCode { get; } = wardCode;
    public string AddressDetail { get; } = addressDetail;
    public string PropertyType { get; } = propertyType;
}
