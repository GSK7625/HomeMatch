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
            table.HasCheckConstraint("CK_UserProfiles_Name", "char_length(btrim(\"full_name\")) BETWEEN 2 AND 100");
        });
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.UserId).ValueGeneratedNever();
        builder.Property(profile => profile.FullName).HasMaxLength(100).IsRequired();
        builder.Ignore(profile => profile.PhoneNumber);
        builder.Property(profile => profile.AvatarUrl).HasMaxLength(500);
        builder.Property(profile => profile.Gender).HasMaxLength(10);
        builder.Property(profile => profile.NotifyEmail).HasDefaultValue(true);
        builder.Property(profile => profile.NotifySms).HasDefaultValue(false);
        builder.Property(profile => profile.NotifyPush).HasDefaultValue(true);
        builder.Ignore(profile => profile.CreatedAt);
        builder.Property(profile => profile.UpdatedAt).IsRequired();
        builder.HasOne<IdentityAccount>().WithOne()
            .HasForeignKey<UserProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}