namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class ListingR2Settings
{
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicCustomDomain { get; set; } = string.Empty;
}
