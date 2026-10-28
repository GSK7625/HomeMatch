using HomeMatch.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    /// <summary>Bảng quản lý tài khoản người dùng, phân quyền vai trò (Chủ nhà, Khách thuê, Admin)</summary>
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users", "identity", table =>
            {
                table.HasCheckConstraint("ck_users_email_length", "length(email) <= 255");
                table.HasCheckConstraint("ck_users_role", "role IN ('TENANT', 'LANDLORD', 'ADMIN')");
                table.HasCheckConstraint("ck_users_status", "status IN ('ACTIVE', 'LOCKED', 'PENDING_VERIFICATION')");
            });
            user.HasKey(x => x.Id);
            user.Property(x => x.Email).HasColumnType("citext").IsRequired();
            user.Property(x => x.PhoneNumber).HasMaxLength(20);
            user.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            user.Property(x => x.Role).HasMaxLength(20).HasDefaultValue("TENANT").IsRequired();
            user.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("ACTIVE").IsRequired();
            user.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            user.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            user.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_users_email");
            user.HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("phone_number IS NOT NULL")
                .HasDatabaseName("ux_users_phone");
        });

        UseSnakeCaseColumns(modelBuilder);
    }

    private static void UseSnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(string.Concat(property.Name.Select((c, i) =>
                char.IsUpper(c) && i > 0 ? $"_{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString())));
    }
}
