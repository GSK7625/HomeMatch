using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence;

public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options), IBillingDbContext
{
    public const string Schema = "billing";

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Landlord Dashboard entities
    public DbSet<DashboardInvoice> DashboardInvoices => Set<DashboardInvoice>();
    public DbSet<BillingContract> Contracts => Set<BillingContract>();
    public DbSet<BillingRoom> Rooms => Set<BillingRoom>();
    public DbSet<BillingViewing> Viewings => Set<BillingViewing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        modelBuilder.Entity<DashboardInvoice>(b =>
        {
            b.ToTable("dashboard_invoices");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.InvoiceCode).HasMaxLength(50).IsRequired();
            b.Property(x => x.BuildingId).HasMaxLength(50).IsRequired();
            b.Property(x => x.RoomName).HasMaxLength(100).IsRequired();
            b.Property(x => x.TenantName).HasMaxLength(150).IsRequired();
            b.Property(x => x.TenantPhone).HasMaxLength(20);
            b.Property(x => x.Period).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<BillingContract>(b =>
        {
            b.ToTable("contracts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.ContractCode).HasMaxLength(50).IsRequired();
            b.Property(x => x.BuildingId).HasMaxLength(50).IsRequired();
            b.Property(x => x.RoomName).HasMaxLength(100).IsRequired();
            b.Property(x => x.TenantName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.DepositStatus).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<BillingRoom>(b =>
        {
            b.ToTable("rooms");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.BuildingId).HasMaxLength(50).IsRequired();
            b.Property(x => x.BuildingName).HasMaxLength(150).IsRequired();
            b.Property(x => x.RoomNumber).HasMaxLength(100).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<BillingViewing>(b =>
        {
            b.ToTable("viewing_appointments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.BuildingId).HasMaxLength(50).IsRequired();
            b.Property(x => x.RoomName).HasMaxLength(100).IsRequired();
            b.Property(x => x.TenantName).HasMaxLength(150).IsRequired();
            b.Property(x => x.TenantPhone).HasMaxLength(20);
            b.Property(x => x.ScheduledTime).HasMaxLength(30);
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        });
    }
}
