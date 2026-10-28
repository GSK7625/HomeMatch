using HomeMatch.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshCredentialConfiguration : IEntityTypeConfiguration<RefreshCredential>
{
    public void Configure(EntityTypeBuilder<RefreshCredential> builder)
    {
        builder.HasKey(credential => credential.Id);
        builder.HasAlternateKey(credential => new { credential.Id, credential.SessionId });
        builder.Property(credential => credential.Id).ValueGeneratedNever();
        builder.Property(credential => credential.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(credential => credential.TokenHash).IsUnique().HasDatabaseName("UX_RefreshCredentials_TokenHash");
        builder.HasIndex(credential => credential.SessionId);
        builder.HasIndex(credential => credential.SessionId, "ActiveCredentialPerSession")
            .IsUnique().HasDatabaseName("UX_RefreshCredentials_ActiveSession")
            .HasFilter("\"UsedAt\" IS NULL AND \"RevokedAt\" IS NULL");
        builder.HasIndex(credential => credential.ReplacedByCredentialId).IsUnique()
            .HasDatabaseName("UX_RefreshCredentials_Replacement").HasFilter("\"ReplacedByCredentialId\" IS NOT NULL");
        builder.HasOne<AccountSession>().WithMany().HasForeignKey(credential => credential.SessionId).OnDelete(DeleteBehavior.Cascade);
        // Composite FK prevents linking a successor from a different session/family.
        builder.HasOne<RefreshCredential>().WithMany()
            .HasForeignKey(credential => new { credential.ReplacedByCredentialId, credential.SessionId })
            .HasPrincipalKey(credential => new { credential.Id, credential.SessionId })
            .OnDelete(DeleteBehavior.NoAction);
        builder.ToTable("RefreshCredentials", table =>
        {
            table.HasCheckConstraint("CK_RefreshCredentials_Hash", "\"TokenHash\" ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint("CK_RefreshCredentials_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
            table.HasCheckConstraint("CK_RefreshCredentials_Used", "\"UsedAt\" IS NULL OR \"UsedAt\" >= \"CreatedAt\"");
            table.HasCheckConstraint("CK_RefreshCredentials_Revoked", "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"CreatedAt\"");
            table.HasCheckConstraint("CK_RefreshCredentials_Replacement",
                "\"ReplacedByCredentialId\" IS NULL OR (\"UsedAt\" IS NOT NULL AND \"ReplacedByCredentialId\" <> \"Id\")");
        });
    }
}
