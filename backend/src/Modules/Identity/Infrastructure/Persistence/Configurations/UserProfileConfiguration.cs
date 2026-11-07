using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.UserId)
            .HasColumnName("user_id");

        builder.Property(p => p.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth");

        builder.Property(p => p.Gender)
            .HasColumnName("gender")
            .HasMaxLength(10);

        builder.Property(p => p.NotifyEmail)
            .HasColumnName("notify_email")
            .HasDefaultValue(true);

        builder.Property(p => p.NotifySms)
            .HasColumnName("notify_sms")
            .HasDefaultValue(false);

        builder.Property(p => p.NotifyPush)
            .HasColumnName("notify_push")
            .HasDefaultValue(true);

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne<ApplicationUser>()
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
