using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application;

internal static class PropertyValidation
{
    public static Error? Validate(string name, string province, string ward, string address, string type) =>
        string.IsNullOrWhiteSpace(name) || name.Length > 200
        || string.IsNullOrWhiteSpace(province) || province.Length > 20
        || string.IsNullOrWhiteSpace(ward) || ward.Length > 20
        || string.IsNullOrWhiteSpace(address) || address.Length > 255
        || type is not ("APARTMENT" or "BOARDING_HOUSE" or "SERVICED_APARTMENT" or "HOMESTAY")
            ? PropertiesErrors.InvalidProperty : null;
}
