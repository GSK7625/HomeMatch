using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Application.DTOs;

/// <summary>Request DTO để tạo mới đơn giá dịch vụ</summary>
public sealed class CreateTariffRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^(ELECTRICITY|WATER|INTERNET|SERVICE|CLEANING)$")]
    public string TariffType { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(PER_KWH|PER_M3|PER_PERSON|FIXED_ROOM)$")]
    public string CalcMethod { get; init; } = string.Empty;

    [Range(typeof(long), "0", "9223372036854775807")]
    public long UnitPrice { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveFrom == default)
            yield return new ValidationResult("Cần nhập ngày hiệu lực.", [nameof(EffectiveFrom)]);
    }
}

/// <summary>Request DTO để cập nhật một phần đơn giá dịch vụ (PATCH)</summary>
public sealed class UpdateTariffRequest
{
    [RegularExpression("^(PER_KWH|PER_M3|PER_PERSON|FIXED_ROOM)$")]
    public string? CalcMethod { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long? UnitPrice { get; init; }

    public DateOnly? EffectiveFrom { get; init; }
}

/// <summary>Response DTO trả về thông tin đơn giá dịch vụ</summary>
public sealed class TariffResponse(
    long id, long propertyId, long? roomId, string tariffType,
    string calcMethod, long unitPrice, DateOnly effectiveFrom)
{
    public long Id { get; } = id;
    public long PropertyId { get; } = propertyId;
    public long? RoomId { get; } = roomId;
    public string TariffType { get; } = tariffType;
    public string CalcMethod { get; } = calcMethod;
    public long UnitPrice { get; } = unitPrice;
    public DateOnly EffectiveFrom { get; } = effectiveFrom;
}
