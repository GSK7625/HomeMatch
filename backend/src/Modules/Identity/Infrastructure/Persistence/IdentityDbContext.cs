using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityAccount, IdentityRole<long>, long>(options)
{
    public const string Schema = "identity";
    public const string MigrationHistoryTable = "__EFMigrationsHistory";

    public DbSet<UserToken> CredentialTokens => Set<UserToken>();
    public DbSet<PublicOtpChallenge> PublicOtpChallenges => Set<PublicOtpChallenge>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<AccountSession> Sessions => Set<AccountSession>();
    public DbSet<RefreshCredential> RefreshCredentials => Set<RefreshCredential>();
    public DbSet<EmailVerificationChallenge> EmailVerificationChallenges => Set<EmailVerificationChallenge>();
    public DbSet<EmailVerificationQuota> EmailVerificationQuotas => Set<EmailVerificationQuota>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<IdentityAccount>().ToTable("Users");
        builder.Ignore<IdentityRole<long>>();
        builder.Ignore<IdentityUserRole<long>>();
        builder.Ignore<IdentityUserClaim<long>>();
        builder.Ignore<IdentityUserLogin<long>>();
        builder.Ignore<IdentityRoleClaim<long>>();
        builder.Ignore<IdentityUserToken<long>>();

        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } tableName) entity.SetTableName(ToSnakeCase(tableName));
            foreach (var property in entity.GetProperties()) property.SetColumnName(ToSnakeCase(property.Name));
        }
    }
    private static string ToSnakeCase(string value) => System.Text.RegularExpressions.Regex
        .Replace(value, "(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
}