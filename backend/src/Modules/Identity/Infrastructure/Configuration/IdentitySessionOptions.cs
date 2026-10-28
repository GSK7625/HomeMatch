namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

public sealed class IdentitySessionOptions
{
    public const string SectionName = "Identity:Sessions";

    public int NormalLifetimeHours { get; set; } = 24;
    public int RememberLifetimeDays { get; set; } = 30;
}
