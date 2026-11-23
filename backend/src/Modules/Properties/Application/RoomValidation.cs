using System.Text.RegularExpressions;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application;

internal static class RoomValidation
{
    private static readonly Regex CodeRegex = new("^[A-Z0-9_-]{1,50}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// Kiểm tra tính hợp lệ của các thông số phòng
    public static Error? Validate(string number, int floor, decimal area, string type, int occupants)
    {
        var normalizedType = (type ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(number) || number.Length > 50
            || floor < 0 || area <= 0 || area > 9999.99m
            || !CodeRegex.IsMatch(normalizedType)
            || occupants <= 0
                ? PropertiesErrors.InvalidRoom
                : null;
    }
}
