namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

/// <summary>JWT settings; Secret is a base64-encoded, randomly generated signing key.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 15;
    public int ClockSkewSeconds { get; set; } = 30;
}
