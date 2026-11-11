using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;

/// <summary>Request DTO để thiết lập / ghi đè toàn bộ đơn giá dịch vụ của bất động sản</summary>
public sealed record SetPropertyTariffsRequest : IValidatableObject
{
    [Required]
    public List<TariffItemRequest> Tariffs { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tariffs is null || Tariffs.Count == 0)
        {
            yield return new ValidationResult("Danh sách biểu giá không được để trống.", [nameof(Tariffs)]);
            yield break;
        }

        var seenTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Tariffs.Count; i++)
        {
            var item = Tariffs[i];
            var prefix = $"{nameof(Tariffs)}[{i}]";

            if (string.IsNullOrWhiteSpace(item.TariffType))
            {
                yield return new ValidationResult("Loại biểu giá không được để trống.", [$"{prefix}.{nameof(item.TariffType)}"]);
            }
            else if (!seenTypes.Add(item.TariffType))
            {
                yield return new ValidationResult($"Loại dịch vụ '{item.TariffType}' bị trùng lặp.", [$"{prefix}.{nameof(item.TariffType)}"]);
            }

            if (item.CalcMethod == "FREE" && item.UnitPrice != 0)
            {
                yield return new ValidationResult("Dịch vụ miễn phí (FREE) thì đơn giá phải bằng 0.", [$"{prefix}.{nameof(item.UnitPrice)}"]);
            }
        }
    }
}

public sealed record TariffItemRequest
{
    [Required]
    [RegularExpression("^(ELECTRICITY|WATER|INTERNET|SERVICE|CLEANING|PARKING)$", ErrorMessage = "Loại biểu giá không hợp lệ.")]
    public string TariffType { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(PER_KWH|PER_M3|PER_PERSON|FIXED_PER_MONTH|FREE)$", ErrorMessage = "Cách tính phí không hợp lệ.")]
    public string CalcMethod { get; init; } = string.Empty;

    [Range(0, 9223372036854775807, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
    public long UnitPrice { get; init; }
}
