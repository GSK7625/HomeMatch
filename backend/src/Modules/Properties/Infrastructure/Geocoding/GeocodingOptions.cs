namespace HomeMatch.Modules.Properties.Infrastructure.Geocoding;

public sealed class GeocodingOptions
{
    // Public Nominatim is for a single-instance, low-traffic trial only. Use a hosted endpoint for production.
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";
    public string UserAgent { get; set; } = "HomeMatch-Platform/1.0";
    public int TimeoutSeconds { get; set; } = 10;
}
