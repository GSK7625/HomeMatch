using HomeMatch.Modules.Leasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Leasing.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts", LeasingDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_contracts_rent", "monthly_rent > 0");
            table.HasCheckConstraint("ck_contracts_deposit", "deposit_amount >= 0");
            table.HasCheckConstraint("ck_contracts_deposit_status", "deposit_status IN ('PENDING', 'PAID', 'REFUNDED', 'FORFEITED')");
            table.HasCheckConstraint("ck_contracts_billing_due_day", "billing_due_day BETWEEN 1 AND 28");
            table.HasCheckConstraint("ck_contracts_date_range", "end_date > start_date");
            table.HasCheckConstraint("ck_contracts_status", "status IN ('DRAFT', 'PENDING_SIGNATURE', 'SIGNATURE_EXPIRED', 'REJECTED', 'SIGNED', 'ACTIVE', 'EXPIRED_PENDING_HANDOVER', 'HANDOVER_OVERDUE', 'COMPLETED', 'TERMINATED', 'CANCELLED')");
            table.HasCheckConstraint("ck_contracts_handover_confirmed", "handover_confirmed_at IS NULL OR handover_confirmed_by IS NOT NULL");
            table.HasCheckConstraint("ck_contracts_actual_occupancy", "actual_occupancy_end_date IS NULL OR actual_occupancy_end_date >= start_date");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.ContractCode)
            .HasColumnName("contract_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.RoomId)
            .HasColumnName("room_id")
            .IsRequired();

        builder.Property(c => c.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(c => c.LandlordId)
            .HasColumnName("landlord_id")
            .IsRequired();

        builder.Property(c => c.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(c => c.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(c => c.MonthlyRent)
            .HasColumnName("monthly_rent")
            .IsRequired();

        builder.Property(c => c.DepositAmount)
            .HasColumnName("deposit_amount")
            .IsRequired();

        builder.Property(c => c.DepositStatus)
            .HasColumnName("deposit_status")
            .HasMaxLength(30)
            .HasDefaultValue("PENDING")
            .IsRequired();

        builder.Property(c => c.BillingDueDay)
            .HasColumnName("billing_due_day")
            .HasDefaultValue(5)
            .IsRequired();

        builder.Property(c => c.ContractFileUrl)
            .HasColumnName("contract_file_url")
            .HasMaxLength(500);

        builder.Property(c => c.DocumentSnapshot)
            .HasColumnName("document_snapshot")
            .HasColumnType("jsonb");

        builder.Property(c => c.DocumentVersion)
            .HasColumnName("document_version")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(c => c.DocumentGeneratedAt)
            .HasColumnName("document_generated_at");

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue("DRAFT")
            .IsRequired();

        builder.Property(c => c.SignatureExpiresAt)
            .HasColumnName("signature_expires_at");

        builder.Property(c => c.LandlordSignedAt)
            .HasColumnName("landlord_signed_at");

        builder.Property(c => c.TenantSignedAt)
            .HasColumnName("tenant_signed_at");

        builder.Property(c => c.SignedDocumentHash)
            .HasColumnName("signed_document_hash")
            .HasMaxLength(64);

        builder.Property(c => c.HandoverDueAt)
            .HasColumnName("handover_due_at");

        builder.Property(c => c.HandoverConfirmedAt)
            .HasColumnName("handover_confirmed_at");

        builder.Property(c => c.HandoverConfirmedBy)
            .HasColumnName("handover_confirmed_by");

        builder.Property(c => c.ActualOccupancyEndDate)
            .HasColumnName("actual_occupancy_end_date");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(c => c.ContractCode)
            .IsUnique()
            .HasDatabaseName("uq_contracts_code");

        builder.HasIndex(c => c.RoomId)
            .HasDatabaseName("ix_contracts_room_id");

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("ix_contracts_tenant_id");

        builder.HasIndex(c => c.LandlordId)
            .HasDatabaseName("ix_contracts_landlord_id");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("ix_contracts_status");
    }
}
