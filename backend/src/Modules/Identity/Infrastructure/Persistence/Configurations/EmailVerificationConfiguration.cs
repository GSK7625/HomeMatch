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
            .HasFilter("\"state\" IN (1, 2)").HasDatabaseName("UX_EmailVerificationChallenges_Active");
        builder.ToTable("EmailVerificationChallenges", table =>
        {
            table.HasCheckConstraint("CK_EmailVerification_Purpose", "\"purpose\" = 'EmailVerification'");
            table.HasCheckConstraint("CK_EmailVerification_Mac", "\"code_mac\" ~ '^[0-9A-F]{64}$'");
            table.HasCheckConstraint("CK_EmailVerification_KeyId", "\"key_id\" ~ '^[A-Za-z0-9_-]{1,32}$'");
            table.HasCheckConstraint("CK_EmailVerification_State", "\"state\" BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_EmailVerification_Time",
                "\"expires_at\" = \"created_at\" + interval '5 minutes' AND \"pending_lease_until\" = \"created_at\" + interval '2 minutes'");
            table.HasCheckConstraint("CK_EmailVerification_Attempts", "\"failed_attempts\" BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_EmailVerification_Submitted",
                "(\"state\" NOT IN (2,4,6) OR \"submitted_at\" IS NOT NULL) AND (\"submitted_at\" IS NULL OR (\"submitted_at\" >= \"created_at\" AND \"submitted_at\" < \"expires_at\"))");
            table.HasCheckConstraint("CK_EmailVerification_Consumed",
                "(\"state\" = 4) = (\"consumed_at\" IS NOT NULL) AND (\"consumed_at\" IS NULL OR (\"consumed_at\" >= \"created_at\" AND \"consumed_at\" < \"expires_at\"))");
            table.HasCheckConstraint("CK_EmailVerification_Invalidated",
                "(\"state\" IN (3,5,6)) = (\"invalidated_at\" IS NOT NULL) AND (\"invalidated_at\" IS NULL OR \"invalidated_at\" >= \"created_at\")");
            table.HasCheckConstraint("CK_EmailVerification_LastAttempt", "\"last_attempt_at\" IS NULL OR \"last_attempt_at\" >= \"created_at\"");
            table.HasCheckConstraint("CK_EmailVerification_Locked", "(\"state\" = 6) = (\"failed_attempts\" = 5)");
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
            table.HasCheckConstraint("CK_EmailVerificationQuota_Window", "\"window_end\" > \"window_start\"");
            table.HasCheckConstraint("CK_EmailVerificationQuota_Count", "\"count\" >= 0");
        });
    }
}
