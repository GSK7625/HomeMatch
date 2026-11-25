using System.Globalization;
using System.Text;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Geocoding;

public sealed class GeocodingHandler(ICatalogReader catalogs, IGeocodingService geocoding)
{
    private static readonly string[] NormalizedAdminPrefixes =
    [
        "thanh pho ", "tp. ", "tp ", "tinh ", "quan ", "huyen ", "thi xa ", "tx. ",
        "phuong ", "xa ", "thi tran ", "tt. ", "p. ", "x. "
    ];

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
        var matched = await MatchAdministrativeAsync(found.Value, latitude.Value, longitude.Value, cancellationToken);
        return Result.Success(matched);
    }

    private async Task<GeocodingResult> MatchAdministrativeAsync(
        GeocodingPlace place, decimal originalLat, decimal originalLng, CancellationToken cancellationToken)
    {
        var provinces = await catalogs.GetProvincesAsync(cancellationToken);
        ProvinceResult? matchedProvince = null;

        var candidateProvinces = provinces.Where(p =>
            place.ProvinceNames.Any(provName => IsAdminMatch(provName, p.Name, p.FullName))).ToList();

        if (candidateProvinces.Count == 1)
        {
            matchedProvince = candidateProvinces[0];
        }

        WardResult? matchedWard = null;
        if (matchedProvince is not null)
        {
            var wards = await catalogs.GetWardsAsync(matchedProvince.Code, cancellationToken);
            var candidateWards = wards.Where(w =>
                place.WardNames.Any(wardName => IsAdminMatch(wardName, w.Name, w.FullName))).ToList();

            if (candidateWards.Count == 1)
            {
                matchedWard = candidateWards[0];
            }
        }

        var matchStatus = (matchedProvince is not null && matchedWard is not null)
            ? "MATCHED"
            : (matchedProvince is not null)
                ? "PARTIAL"
                : "NEEDS_CONFIRMATION";

        return new GeocodingResult(
            originalLat,
            originalLng,
            place.FormattedAddress,
            place.AddressDetail,
            matchedProvince?.Code,
            matchedProvince?.FullName ?? place.ProvinceNames.FirstOrDefault(),
            matchedWard?.Code,
            matchedWard?.FullName ?? place.WardNames.FirstOrDefault(),
            matchStatus,
            place.Precision);
    }

    private static bool IsAdminMatch(string providerName, string catalogName, string catalogFullName)
    {
        var normProvider = NormalizeAdmin(providerName);
        if (string.IsNullOrEmpty(normProvider)) return false;

        var normName = NormalizeAdmin(catalogName);
        var normFullName = NormalizeAdmin(catalogFullName);

        return normProvider == normName || normProvider == normFullName;
    }

    private static string NormalizeAdmin(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var s = RemoveDiacritics(text.Trim().ToLowerInvariant());
        foreach (var prefix in NormalizedAdminPrefixes)
        {
            if (s.StartsWith(prefix))
            {
                s = s[prefix.Length..].Trim();
                break;
            }
        }
        return s.Replace(" ", "");
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
    }

    private static bool IsVietnamPlace(GeocodingPlace place) =>
        place.CountryCode.Equals("vn", StringComparison.OrdinalIgnoreCase)
        && place.Latitude is >= -90 and <= 90 && place.Longitude is >= -180 and <= 180;

    private static GeocodingResult ToResult(GeocodingPlace place) =>
        new(place.Latitude, place.Longitude, place.FormattedAddress, place.AddressDetail,
            null, place.ProvinceNames.FirstOrDefault(), null, place.WardNames.FirstOrDefault(),
            "NEEDS_CONFIRMATION", place.Precision);
}
