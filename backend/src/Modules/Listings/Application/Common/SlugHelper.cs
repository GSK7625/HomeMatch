using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HomeMatch.Modules.Listings.Application.Common;

public static class SlugHelper
{
    private static readonly Regex NonAlphanumeric = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Generate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            title = "tin-dang";

        var normalized = RemoveVietnameseDiacritics(title).ToLowerInvariant();
        var slug = NonAlphanumeric.Replace(normalized, "-").Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
            slug = "phong-tro";

        if (slug.Length > 200)
            slug = slug[..200].TrimEnd('-');

        var suffix = Guid.NewGuid().ToString("n")[..6];
        return $"{slug}-{suffix}";
    }

    public static string RemoveVietnameseDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder();
        var normalized = text.Normalize(NormalizationForm.FormD);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c switch
                {
                    'Đ' => 'D',
                    'đ' => 'd',
                    _ => c
                });
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
