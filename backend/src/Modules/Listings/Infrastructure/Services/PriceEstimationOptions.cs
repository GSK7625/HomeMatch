namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class PriceEstimationOptions
{
    public const string SectionName = "PriceModel";

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string InternalApiKey { get; set; } = "homematch-internal-secret";
}
