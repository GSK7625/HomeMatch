using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityAccount, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "identity";
    public const string MigrationHistoryTable = "__EFMigrationsHistory";

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
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<IdentityRole<Guid>>().Property(role => role.NormalizedName).IsRequired();
        // The first release supports exactly one business role per account.
        builder.Entity<IdentityUserRole<Guid>>().HasIndex(role => role.UserId)
            .IsUnique().HasDatabaseName("UX_UserRoles_UserId");

        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}