using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityAccountConfiguration : IEntityTypeConfiguration<IdentityAccount>
{
    public void Configure(EntityTypeBuilder<IdentityAccount> builder)
    {
        builder.Property(user => user.Id).UseIdentityByDefaultColumn();
        builder.Property(user => user.Email).HasMaxLength(255).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(255).IsRequired();
        builder.Property(user => user.UserName).HasMaxLength(255).IsRequired();
        builder.Property(user => user.NormalizedUserName).HasMaxLength(255).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(20);
        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName("uq_users_email");
        builder.HasIndex(user => user.PhoneNumber).IsUnique().HasFilter("\"phone_number\" IS NOT NULL").HasDatabaseName("uq_users_phone");
        builder.Property(user => user.SecurityStamp).IsRequired();
        builder.Property(user => user.ConcurrencyStamp).IsRequired();
        builder.Property(user => user.Role).HasMaxLength(20).HasConversion(
            value => value.ToUpperInvariant(), value => value == "TENANT" ? "Tenant" : value == "LANDLORD" ? "Landlord" : value == "ADMIN" ? "Admin" : value);
        builder.Property(user => user.Status).HasMaxLength(20).HasConversion(
            value => value == HomeMatch.Modules.Identity.Domain.Enums.AccountStatus.Active ? "ACTIVE" : value == HomeMatch.Modules.Identity.Domain.Enums.AccountStatus.Inactive ? "INACTIVE" : value == HomeMatch.Modules.Identity.Domain.Enums.AccountStatus.Suspended ? "LOCKED" : value.ToString(),
            value => value == "ACTIVE" ? HomeMatch.Modules.Identity.Domain.Enums.AccountStatus.Active : value == "INACTIVE" ? HomeMatch.Modules.Identity.Domain.Enums.AccountStatus.Inactive : HomeMatch.Modules.Identity.Domain.Enums.AccountStatus.Suspended);
        builder.Property(user => user.SecurityVersion).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();

        // UserManager's lookup is insufficient under a race; the database is the final guard.
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique().HasDatabaseName("UX_Users_NormalizedEmail");

        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint("CK_Users_Status", "\"status\" IN ('ACTIVE', 'INACTIVE', 'LOCKED')");
            table.HasCheckConstraint("CK_Users_SecurityVersion", "\"security_version\" > 0");
            table.HasCheckConstraint("CK_Users_Timestamps", "\"updated_at\" >= \"created_at\"");
        });
    }
}