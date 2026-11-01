using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class PublicOtpConfiguration : IEntityTypeConfiguration<PublicOtpChallenge>
{
    public void Configure(EntityTypeBuilder<PublicOtpChallenge> b)
    {
        b.ToTable("PublicOtpChallenges"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Purpose).HasMaxLength(32);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.CodeMac).HasMaxLength(64);
        b.Property(x => x.KeyId).HasMaxLength(32);
        b.Property(x => x.ResetTokenHash).HasMaxLength(64);
        b.HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAt });
        b.HasIndex(x => x.ResetTokenHash).IsUnique();
        b.HasOne<IdentityAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
