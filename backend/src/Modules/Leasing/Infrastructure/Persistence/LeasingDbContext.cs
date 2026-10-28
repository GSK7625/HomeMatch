using HomeMatch.Modules.Leasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Persistence;

public class LeasingDbContext : DbContext
{
    public LeasingDbContext(DbContextOptions<LeasingDbContext> options) : base(options)
    {
    }

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ContractAuditLog> ContractAuditLogs => Set<ContractAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema isolation for Modular Monolith
        modelBuilder.HasDefaultSchema("leasing");

        // Contract entity configuration
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("Contracts");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ContractCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(c => c.ContractCode).IsUnique();

            entity.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(c => c.DepositStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(c => c.MonthlyRent).IsRequired();
            entity.Property(c => c.DepositAmount).IsRequired();
            entity.Property(c => c.BillingDueDay).HasDefaultValue(5);

            entity.HasIndex(c => c.LandlordId);
            entity.HasIndex(c => c.TenantId);
            entity.HasIndex(c => c.RoomId);
            entity.HasIndex(c => c.Status);
        });

        // Lead entity configuration
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(l => new { l.TenantId, l.ListingId }).IsUnique();
            entity.HasIndex(l => l.ListingId);
            entity.HasIndex(l => l.Status);
        });

        // ContractAuditLog configuration
        modelBuilder.Entity<ContractAuditLog>(entity =>
        {
            entity.ToTable("ContractAuditLogs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            entity.Property(a => a.PerformedByRole).HasMaxLength(30).IsRequired();
            entity.HasIndex(a => a.ContractId);
        });
    }
}
