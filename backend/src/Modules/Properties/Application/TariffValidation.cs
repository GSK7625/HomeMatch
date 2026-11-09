using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application;

internal static class TariffValidation
{
    /// Kiểm tra loại phí và phương thức tính toán hợp lệ
    public static Error? Validate(string type, string method, long price)
    {
        var valid = type switch
        {
            "ELECTRICITY" => method == "PER_KWH",
            "WATER" => method is "PER_M3" or "PER_PERSON" or "FIXED_PER_MONTH",
            "INTERNET" or "SERVICE" or "CLEANING" or "PARKING" =>
                method is "PER_PERSON" or "FIXED_PER_MONTH" or "FREE",
            _ => false
        };
        return !valid || price < 0 ? PropertiesErrors.InvalidTariff : null;
    }
}
