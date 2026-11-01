namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

public sealed class IdentityDatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = string.Empty;
}