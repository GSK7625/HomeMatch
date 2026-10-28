namespace HomeMatch.Api.Configuration;

public sealed class FrontendCorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "AllowFrontend";

    public string[] AllowedOrigins { get; set; } = [];
}
