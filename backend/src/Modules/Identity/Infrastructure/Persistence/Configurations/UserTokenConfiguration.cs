using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.UseTptMappingStrategy();
        builder.ToTable("user_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).UseIdentityByDefaultColumn();
        builder.Property(token => token.TokenHash).HasColumnType("character(64)").IsRequired();
        builder.Property(token => token.TokenType).HasMaxLength(30).IsRequired();
        builder.Property(token => token.DeviceInfo).HasMaxLength(255);
        builder.Property(token => token.IpAddress).HasMaxLength(45);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasOne<IdentityAccount>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
