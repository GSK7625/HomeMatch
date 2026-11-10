using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using HomeMatch.Modules.Properties.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Properties.Infrastructure.Geocoding;

public sealed class NominatimAddressGeocoder(
    IHttpClientFactory clients,
    IOptions<GeocodingOptions> options,
    ILogger<NominatimAddressGeocoder> logger) : IAddressGeocoder, IDisposable
{
    public const string ClientName = "Properties.Geocoding";
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 512 });
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long? _lastRequest;

    public async Task<GeocodedLocation?> FindAsync(string address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_cache.TryGetValue(address, out GeocodedLocation? cached))
            return cached;

        var candidates = GetAddressCandidates(address);
        GeocodedLocation? location = null;

        foreach (var candidate in candidates)
        {
            var (isFailure, candidateLocation) = await QueryCandidateAsync(candidate, cancellationToken);
            if (isFailure)
            {
                // Network or HTTP provider failure; do not retry other candidates
                return null;
            }

            if (candidateLocation is not null)
            {
                location = candidateLocation;
                break;
            }
        }

        _cache.Set(address, location, new MemoryCacheEntryOptions
        {
            Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
        return location;
    }

    private static IEnumerable<string> GetAddressCandidates(string address)
    {
        yield return address;

        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Address is commonly: [addressDetail, Ward, District, Province, "Việt Nam"]
        // If there are at least 4 parts, try falling back to [Ward, District, Province, "Việt Nam"]
        if (parts.Length >= 4)
        {
            yield return string.Join(", ", parts.Skip(1));
        }

        // If there are at least 5 parts, try falling back to [District, Province, "Việt Nam"]
        if (parts.Length >= 5)
        {
            yield return string.Join(", ", parts.Skip(2));
        }
    }

    private async Task<(bool IsFailure, GeocodedLocation? Location)> QueryCandidateAsync(
        string query, CancellationToken cancellationToken)
    {
        // shortcut: one API process; multiple replicas need a shared proxy/rate limit for public Nominatim.
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_lastRequest is { } last)
            {
                var delay = TimeSpan.FromMilliseconds(1100) - Stopwatch.GetElapsedTime(last);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);
            }

            var settings = options.Value;
            var uri = new Uri(new Uri(settings.BaseUrl),
                "search?format=jsonv2&countrycodes=vn&addressdetails=1&limit=2&q="
                + Uri.EscapeDataString(query));
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(settings.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.ParseAdd("vi");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            _lastRequest = Stopwatch.GetTimestamp();
            using var response = await clients.CreateClient(ClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Geocoding provider returned HTTP {StatusCode}.", (int)response.StatusCode);
                return (true, null);
            }

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: timeout.Token);
            var location = ReadLocation(document.RootElement);
            return (false, location);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Geocoding provider timed out.");
            return (true, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException)
        {
            // Do not log the submitted address or the provider response.
            logger.LogWarning("Geocoding provider failed ({FailureType}).", exception.GetType().Name);
            return (true, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static GeocodedLocation? ReadLocation(JsonElement results)
    {
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            return null;

        GeocodedLocation? bestMatch = null;
        var highestRank = -1;

        foreach (var result in results.EnumerateArray())
        {
            if (result.ValueKind != JsonValueKind.Object
                || !result.TryGetProperty("place_rank", out var rank)
                || rank.ValueKind != JsonValueKind.Number
                || !rank.TryGetInt32(out var placeRank) || placeRank < 20
                || !result.TryGetProperty("lat", out var lat) || lat.ValueKind != JsonValueKind.String
                || !result.TryGetProperty("lon", out var lon) || lon.ValueKind != JsonValueKind.String
                || !decimal.TryParse(lat.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !decimal.TryParse(lon.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                || latitude is < -90 or > 90 || longitude is < -180 or > 180)
            {
                continue;
            }

            if (placeRank > highestRank)
            {
                highestRank = placeRank;
                bestMatch = new(decimal.Round(latitude, 6), decimal.Round(longitude, 6));
            }
        }

        return bestMatch;
    }

    public void Dispose()
    {
        _cache.Dispose();
        _gate.Dispose();
    }
}
