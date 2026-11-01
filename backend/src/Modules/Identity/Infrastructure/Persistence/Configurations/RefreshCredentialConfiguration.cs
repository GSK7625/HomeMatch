using HomeMatch.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshCredentialConfiguration : IEntityTypeConfiguration<RefreshCredential>
{
    public void Configure(EntityTypeBuilder<RefreshCredential> builder)
    {
        builder.HasIndex(credential => credential.SessionId);
        builder.HasIndex(credential => credential.SessionId, "ActiveCredentialPerSession")
            .IsUnique().HasDatabaseName("UX_RefreshCredentials_ActiveSession")
            .HasFilter("\"used_at\" IS NULL AND \"revoked_at\" IS NULL");
        builder.HasIndex(credential => credential.ReplacedByCredentialId).IsUnique()
            .HasDatabaseName("UX_RefreshCredentials_Replacement").HasFilter("\"replaced_by_credential_id\" IS NOT NULL");
        builder.HasOne<AccountSession>().WithMany().HasForeignKey(credential => credential.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RefreshCredential>().WithMany()
            .HasForeignKey(credential => credential.ReplacedByCredentialId).OnDelete(DeleteBehavior.NoAction);
        builder.ToTable("refresh_credential_metadata");
    }
}
