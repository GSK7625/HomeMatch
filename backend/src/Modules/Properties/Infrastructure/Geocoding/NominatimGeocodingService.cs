using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HomeMatch.Modules.Properties.Application;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Properties.Infrastructure.Geocoding;

public sealed class NominatimGeocodingService(
    IHttpClientFactory clients, IOptionsMonitor<GeocodingOptions> options, ILogger<NominatimGeocodingService> logger)
    : IGeocodingService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 512 });
    private long _lastRequest;

    public Task<Result<IReadOnlyList<GeocodingPlace>>> SearchAsync(string address, CancellationToken cancellationToken) =>
        RequestAsync($"search?format=jsonv2&countrycodes=vn&limit=5&addressdetails=1&q={Uri.EscapeDataString(address)}", false, cancellationToken);

    public async Task<Result<GeocodingPlace>> ReverseAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        var result = await RequestAsync(FormattableString.Invariant(
            $"reverse?format=jsonv2&lat={latitude}&lon={longitude}&addressdetails=1&zoom=18&layer=address"), true, cancellationToken);
        return result.IsFailure ? Result.Failure<GeocodingPlace>(result.Error)
            : result.Value.Count == 0 ? Result.Failure<GeocodingPlace>(PropertiesErrors.GeocodingNotFound)
            : Result.Success(result.Value[0]);
    }

    private async Task<Result<IReadOnlyList<GeocodingPlace>>> RequestAsync(string path, bool reverse, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != "https"
            || string.IsNullOrWhiteSpace(settings.UserAgent))
            return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingUnavailable);
        var uri = new Uri(new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/"), path);
        var cacheKey = uri.AbsoluteUri;
        if (_cache.TryGetValue<IReadOnlyList<GeocodingPlace>>(cacheKey, out var cached) && cached is not null)
            return Result.Success(cached);

        // Reject excess concurrent misses rather than creating an unbounded queue of provider calls.
        if (!await _gate.WaitAsync(0, cancellationToken))
            return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingBusy);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 1, 30)));
            var token = timeout.Token;
            // One shared gate covers forward and reverse. shortcut: public quota requires a single API instance;
            // switch to a hosted endpoint before scaling out (the public limit applies to the whole application).
            if (_lastRequest != 0)
            {
                var delay = TimeSpan.FromMilliseconds(1100) - Stopwatch.GetElapsedTime(_lastRequest);
                if (delay > TimeSpan.Zero) await Task.Delay(delay, token);
            }
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("User-Agent", settings.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept-Language", "vi,en;q=0.8");
            _lastRequest = Stopwatch.GetTimestamp();
            using var response = await clients.CreateClient("Properties.Geocoding").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingNotFound);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Geocoding provider returned HTTP {StatusCode}", (int)response.StatusCode);
                return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingUnavailable);
            }
            await response.Content.LoadIntoBufferAsync(262144, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var root = document.RootElement;
            if (reverse && root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out _))
                return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingNotFound);
            var items = reverse
                ? new[] { root.Deserialize<NominatimPlace>() ?? throw new JsonException() }
                : root.Deserialize<NominatimPlace[]>() ?? throw new JsonException();
            IReadOnlyList<GeocodingPlace> places = items.Take(5).Select(Map).Where(p => p is not null).Cast<GeocodingPlace>().ToArray();
            if (items.Length > 0 && places.Count == 0)
                return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingUnavailable);
            _cache.Set(cacheKey, places, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
            return Result.Success(places);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingUnavailable);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            // Do not log the URL, address or provider body (locations may be private).
            logger.LogWarning("Geocoding provider failed with {FailureType}", exception.GetType().Name);
            return Result.Failure<IReadOnlyList<GeocodingPlace>>(PropertiesErrors.GeocodingUnavailable);
        }
        finally { _gate.Release(); }
    }

    private static GeocodingPlace? Map(NominatimPlace? place)
    {
        if (place is null || !decimal.TryParse(place.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !decimal.TryParse(place.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
            || lat is < -90 or > 90 || lon is < -180 or > 180 || string.IsNullOrWhiteSpace(place.DisplayName)
            || place.Address is null) return null;
        string Read(string key) => place.Address.GetValueOrDefault(key) ?? "";
        string[] Names(params string[] keys) => keys.Select(Read).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray();

        // Strip postal codes (4-6 digits, e.g. "13906") from display name
        var cleanedDisplayName = Regex.Replace(place.DisplayName, @",\s*\d{4,6}(?=,|$)", "").Trim();

        var houseNumber = Read("house_number");
        var road = Names("road", "pedestrian", "residential", "path", "street").FirstOrDefault() ?? "";
        var hamlet = Names("hamlet", "neighbourhood").FirstOrDefault() ?? "";

        var roadPart = string.Join(" ", new[] { houseNumber, road }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var detail = string.Join(", ", new[] { roadPart, hamlet }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var precision = place.AddressType is "house" or "building" or "apartments" ? "BUILDING"
            : place.AddressType is "road" or "pedestrian" or "residential" ? "STREET" : "AREA";

        var wardNames = Names("commune", "suburb", "quarter", "village", "town", "city_district", "municipality", "county", "district");

        return new(lat, lon, cleanedDisplayName, detail, Names("state", "province", "city"),
            wardNames, Read("country_code"), precision);
    }

    public void Dispose() { _cache.Dispose(); _gate.Dispose(); }

    private sealed class NominatimPlace
    {
        [JsonPropertyName("lat")] public string? Lat { get; init; }
        [JsonPropertyName("lon")] public string? Lon { get; init; }
        [JsonPropertyName("display_name")] public string? DisplayName { get; init; }
        [JsonPropertyName("addresstype")] public string? AddressType { get; init; }
        [JsonPropertyName("address")] public Dictionary<string, string>? Address { get; init; }
    }
}
