namespace HomeMatch.Modules.Properties.Application.DTOs;

public sealed record GeocodingPlace(
    decimal Latitude, decimal Longitude, string FormattedAddress, string AddressDetail,
    IReadOnlyList<string> ProvinceNames, IReadOnlyList<string> WardNames, string CountryCode, string Precision);

public sealed record GeocodingResult(
    decimal Latitude, decimal Longitude, string FormattedAddress, string AddressDetail,
    string? ProvinceCode, string? ProvinceName, string? WardCode, string? WardName,
    string MatchStatus, string Precision);
