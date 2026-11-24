using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Geocoding;

public sealed class GeocodingHandler(ICatalogReader catalogs, IGeocodingService geocoding)
{
    public async Task<Result<IReadOnlyList<GeocodingResult>>> SearchAsync(
        string? addressDetail, string? provinceCode, string? wardCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(addressDetail) || addressDetail.Trim().Length > 255
            || string.IsNullOrWhiteSpace(provinceCode) || provinceCode.Length > 20
            || string.IsNullOrWhiteSpace(wardCode) || wardCode.Length > 20)
            return Result.Failure<IReadOnlyList<GeocodingResult>>(PropertiesErrors.InvalidGeocodingInput);

        var provinces = await catalogs.GetProvincesAsync(cancellationToken);
        var province = provinces.SingleOrDefault(p => p.Code == provinceCode.Trim());
        if (province is null) return Result.Failure<IReadOnlyList<GeocodingResult>>(PropertiesErrors.InvalidAddress);
        var wards = await catalogs.GetWardsAsync(province.Code, cancellationToken);
        var ward = wards.SingleOrDefault(w => w.Code == wardCode.Trim());
        if (ward is null) return Result.Failure<IReadOnlyList<GeocodingResult>>(PropertiesErrors.InvalidAddress);

        var address = $"{addressDetail.Trim()}, {ward.FullName}, {province.FullName}, Việt Nam";
        var found = await geocoding.SearchAsync(address, cancellationToken);
        if (found.IsFailure) return Result.Failure<IReadOnlyList<GeocodingResult>>(found.Error);
        var results = found.Value.Where(IsVietnamPlace).Select(ToResult).ToList();
        return results.Count == 0
            ? Result.Failure<IReadOnlyList<GeocodingResult>>(PropertiesErrors.GeocodingNotFound)
            : Result.Success<IReadOnlyList<GeocodingResult>>(results);
    }

    public async Task<Result<GeocodingResult>> ReverseAsync(
        decimal? latitude, decimal? longitude, CancellationToken cancellationToken)
    {
        var error = PropertyValidation.ValidateCoordinates(latitude, longitude, required: true);
        if (error is not null) return Result.Failure<GeocodingResult>(error);
        var found = await geocoding.ReverseAsync(latitude!.Value, longitude!.Value, cancellationToken);
        if (found.IsFailure) return Result.Failure<GeocodingResult>(found.Error);
        if (!IsVietnamPlace(found.Value)) return Result.Failure<GeocodingResult>(PropertiesErrors.GeocodingNotFound);
        // Reverse results describe a nearby OSM object; never move the user's pin to that object's coordinates.
        return Result.Success(ToResult(found.Value) with { Latitude = latitude.Value, Longitude = longitude.Value });
    }

    private static bool IsVietnamPlace(GeocodingPlace place) =>
        place.CountryCode.Equals("vn", StringComparison.OrdinalIgnoreCase)
        && place.Latitude is >= -90 and <= 90 && place.Longitude is >= -180 and <= 180;

    private static GeocodingResult ToResult(GeocodingPlace place) =>
        new(place.Latitude, place.Longitude, place.FormattedAddress, place.AddressDetail,
            null, place.ProvinceNames.FirstOrDefault(), null, place.WardNames.FirstOrDefault(),
            "NEEDS_CONFIRMATION", place.Precision);
}
