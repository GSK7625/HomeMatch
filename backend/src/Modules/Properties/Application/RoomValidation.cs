using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application;

internal static class RoomValidation
{
    /// Kiểm tra tính hợp lệ của các thông số phòng
    public static Error? Validate(string number, int floor, decimal area, string type, int occupants, long price) =>
        string.IsNullOrWhiteSpace(number) || number.Length > 50
        || floor < 0 || area <= 0 || area > 9999.99m
        || type is not ("SINGLE" or "STUDIO" or "1B1B" or "2B1B" or "DORMITORY")
        || occupants <= 0 || price < 0 ? PropertiesErrors.InvalidRoom : null;
}
