using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityAccountConfiguration : IEntityTypeConfiguration<IdentityAccount>
{
    public void Configure(EntityTypeBuilder<IdentityAccount> builder)
    {
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).HasMaxLength(254).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(user => user.UserName).HasMaxLength(254).IsRequired();
        builder.Property(user => user.NormalizedUserName).HasMaxLength(254).IsRequired();
        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.SecurityStamp).IsRequired();
        builder.Property(user => user.ConcurrencyStamp).IsRequired();
        builder.Property(user => user.Status).HasConversion<short>();
        builder.Property(user => user.SecurityVersion).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();

        // UserManager's lookup is insufficient under a race; the database is the final guard.
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique().HasDatabaseName("UX_Users_NormalizedEmail");

        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint("CK_Users_Status", "\"Status\" IN (1, 2)");
            table.HasCheckConstraint("CK_Users_SecurityVersion", "\"SecurityVersion\" > 0");
            table.HasCheckConstraint("CK_Users_Timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
        });
    }
}