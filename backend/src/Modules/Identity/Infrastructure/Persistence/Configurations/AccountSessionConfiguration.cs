using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class AccountSessionConfiguration : IEntityTypeConfiguration<AccountSession>
{
    public void Configure(EntityTypeBuilder<AccountSession> builder)
    {
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.HasIndex(session => session.UserId);
        builder.HasOne<IdentityAccount>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("Sessions", table =>
        {
            table.HasCheckConstraint("CK_Sessions_SecurityVersion", "\"SecurityVersion\" > 0");
            table.HasCheckConstraint("CK_Sessions_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
            table.HasCheckConstraint("CK_Sessions_Revocation", "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"CreatedAt\"");
        });
    }
}
