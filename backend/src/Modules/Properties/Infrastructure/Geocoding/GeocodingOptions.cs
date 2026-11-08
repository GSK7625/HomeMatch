namespace HomeMatch.Modules.Properties.Infrastructure.Geocoding;

public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";
    public string UserAgent { get; set; } = "HomeMatch/1.0";
    public int TimeoutSeconds { get; set; } = 10;
}
