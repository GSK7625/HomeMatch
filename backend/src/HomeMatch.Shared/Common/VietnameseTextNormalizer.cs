using System.Globalization;
using System.Text;

namespace HomeMatch.Shared.Common;

/// <summary>
/// Hỗ trợ chuẩn hóa tiếng Việt: loại bỏ dấu, chuyển về chữ thường để phục vụ tìm kiếm không phân biệt dấu.
/// Ví dụ: "Trần Đại Nghĩa" -> "tran dai nghia", "Đống Đa" -> "dong da".
/// </summary>
public static class VietnameseTextNormalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var normalized = input.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .ToLowerInvariant();
    }
}
