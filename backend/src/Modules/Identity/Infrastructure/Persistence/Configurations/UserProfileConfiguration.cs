using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles", table =>
        {
            table.HasCheckConstraint("CK_UserProfiles_Name", "char_length(btrim(\"FullName\")) BETWEEN 2 AND 100");
            table.HasCheckConstraint("CK_UserProfiles_Timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
        });
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.UserId).ValueGeneratedNever();
        builder.Property(profile => profile.FullName).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.PhoneNumber).HasMaxLength(16);
        builder.Property(profile => profile.CreatedAt).IsRequired();
        builder.Property(profile => profile.UpdatedAt).IsRequired();
        builder.HasOne<IdentityAccount>().WithOne()
            .HasForeignKey<UserProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}