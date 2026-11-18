using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", BillingDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_invoices_status", "status IN ('UNPAID', 'OVERDUE', 'PAID', 'CANCELLED')");
            table.HasCheckConstraint("ck_invoices_month", "billing_month BETWEEN 1 AND 12");
            table.HasCheckConstraint("ck_invoices_year", "billing_year >= 2020");
            table.HasCheckConstraint("ck_invoices_rent", "rent_amount >= 0");
            table.HasCheckConstraint("ck_invoices_paid_limit", "paid_amount <= total_amount");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(i => i.InvoiceCode)
            .HasColumnName("invoice_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.ContractId)
            .HasColumnName("contract_id")
            .IsRequired();

        builder.Property(i => i.LandlordId)
            .HasColumnName("landlord_id")
            .IsRequired();

        builder.Property(i => i.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(i => i.RoomId)
            .HasColumnName("room_id")
            .IsRequired();

        builder.Property(i => i.PropertyId)
            .HasColumnName("property_id")
            .IsRequired();

        builder.Property(i => i.BillingMonth)
            .HasColumnName("billing_month")
            .IsRequired();

        builder.Property(i => i.BillingYear)
            .HasColumnName("billing_year")
            .IsRequired();

        builder.Property(i => i.RentAmount)
            .HasColumnName("rent_amount")
            .IsRequired();

        builder.Property(i => i.ElectricityAmount)
            .HasColumnName("electricity_amount")
            .HasDefaultValue(0L);

        builder.Property(i => i.WaterAmount)
            .HasColumnName("water_amount")
            .HasDefaultValue(0L);

        builder.Property(i => i.ServiceAmount)
            .HasColumnName("service_amount")
            .HasDefaultValue(0L);

        builder.Property(i => i.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasDefaultValue(0L);

        builder.Property(i => i.TotalAmount)
            .HasColumnName("total_amount")
            .IsRequired();

        builder.Property(i => i.PaidAmount)
            .HasColumnName("paid_amount")
            .HasDefaultValue(0L);

        builder.Property(i => i.DueDate)
            .HasColumnName("due_date")
            .IsRequired();

        builder.Property(i => i.PaidAt)
            .HasColumnName("paid_at");

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(InvoiceStatus.Unpaid)
            .IsRequired();

        builder.Property(i => i.CancelReason)
            .HasColumnName("cancel_reason")
            .HasMaxLength(255);

        builder.Property(i => i.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(i => i.InvoiceCode)
            .IsUnique()
            .HasDatabaseName("uq_invoices_code");

        // Partial unique index đảm bảo mỗi hợp đồng chỉ có tối đa 1 hóa đơn chưa hủy cho mỗi kỳ (BR-BIL-07, BR-BIL-16)
        builder.HasIndex(i => new { i.ContractId, i.BillingMonth, i.BillingYear })
            .IsUnique()
            .HasFilter("status <> 'CANCELLED'")
            .HasDatabaseName("uq_invoices_contract_period_active");

        builder.HasIndex(i => new { i.TenantId, i.Status })
            .HasDatabaseName("ix_invoices_tenant_status");

        builder.HasIndex(i => new { i.LandlordId, i.PropertyId, i.DueDate })
            .HasDatabaseName("ix_invoices_landlord_property");

        // Relationship
        builder.HasMany(i => i.Payments)
            .WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
