using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationChallengeConfiguration : IEntityTypeConfiguration<EmailVerificationChallenge>
{
    public void Configure(EntityTypeBuilder<EmailVerificationChallenge> builder)
    {
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.Purpose).HasMaxLength(32).IsRequired();
        builder.Property(value => value.EmailSnapshot).HasMaxLength(254).IsRequired();
        builder.Property(value => value.NormalizedEmailSnapshot).HasMaxLength(254).IsRequired();
        builder.Property(value => value.CodeMac).HasMaxLength(64).IsRequired();
        builder.Property(value => value.KeyId).HasMaxLength(32).IsRequired();
        builder.HasOne<IdentityAccount>().WithMany().HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(value => new { value.UserId, value.CreatedAt });
        builder.HasIndex(value => new { value.UserId, value.Purpose }).IsUnique()
            .HasFilter("\"State\" IN (1, 2)").HasDatabaseName("UX_EmailVerificationChallenges_Active");
        builder.ToTable("EmailVerificationChallenges", table =>
        {
            table.HasCheckConstraint("CK_EmailVerification_Purpose", "\"Purpose\" = 'EmailVerification'");
            table.HasCheckConstraint("CK_EmailVerification_Mac", "\"CodeMac\" ~ '^[0-9A-F]{64}$'");
            table.HasCheckConstraint("CK_EmailVerification_KeyId", "\"KeyId\" ~ '^[A-Za-z0-9_-]{1,32}$'");
            table.HasCheckConstraint("CK_EmailVerification_State", "\"State\" BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_EmailVerification_Time",
                "\"ExpiresAt\" = \"CreatedAt\" + interval '5 minutes' AND \"PendingLeaseUntil\" = \"CreatedAt\" + interval '2 minutes'");
            table.HasCheckConstraint("CK_EmailVerification_Attempts", "\"FailedAttempts\" BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_EmailVerification_Submitted",
                "(\"State\" NOT IN (2,4,6) OR \"SubmittedAt\" IS NOT NULL) AND (\"SubmittedAt\" IS NULL OR (\"SubmittedAt\" >= \"CreatedAt\" AND \"SubmittedAt\" < \"ExpiresAt\"))");
            table.HasCheckConstraint("CK_EmailVerification_Consumed",
                "(\"State\" = 4) = (\"ConsumedAt\" IS NOT NULL) AND (\"ConsumedAt\" IS NULL OR (\"ConsumedAt\" >= \"CreatedAt\" AND \"ConsumedAt\" < \"ExpiresAt\"))");
            table.HasCheckConstraint("CK_EmailVerification_Invalidated",
                "(\"State\" IN (3,5,6)) = (\"InvalidatedAt\" IS NOT NULL) AND (\"InvalidatedAt\" IS NULL OR \"InvalidatedAt\" >= \"CreatedAt\")");
            table.HasCheckConstraint("CK_EmailVerification_LastAttempt", "\"LastAttemptAt\" IS NULL OR \"LastAttemptAt\" >= \"CreatedAt\"");
            table.HasCheckConstraint("CK_EmailVerification_Locked", "(\"State\" = 6) = (\"FailedAttempts\" = 5)");
        });
    }
}

public sealed class EmailVerificationQuotaConfiguration : IEntityTypeConfiguration<EmailVerificationQuota>
{
    public void Configure(EntityTypeBuilder<EmailVerificationQuota> builder)
    {
        builder.HasKey(value => value.Key);
        builder.Property(value => value.Key).HasMaxLength(160).ValueGeneratedNever();
        builder.ToTable("EmailVerificationQuotas", table =>
        {
            table.HasCheckConstraint("CK_EmailVerificationQuota_Window", "\"WindowEnd\" > \"WindowStart\"");
            table.HasCheckConstraint("CK_EmailVerificationQuota_Count", "\"Count\" >= 0");
        });
    }
}
