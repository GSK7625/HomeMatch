using HomeMatch.Modules.Leasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Leasing.Infrastructure.Persistence.Configurations;

public class DepositTransactionConfiguration : IEntityTypeConfiguration<DepositTransaction>
{
    public void Configure(EntityTypeBuilder<DepositTransaction> builder)
    {
        builder.ToTable("deposit_transactions", LeasingDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_deposit_tx_type", "transaction_type IN ('RECEIVED', 'REFUNDED', 'DEDUCTED')");
            table.HasCheckConstraint("ck_deposit_tx_amount", "amount > 0");
            table.HasCheckConstraint("ck_deposit_tx_deduction_reason", "transaction_type <> 'DEDUCTED' OR (reason IS NOT NULL AND length(btrim(reason)) > 0)");
        });

        builder.HasKey(dt => dt.Id);

        builder.Property(dt => dt.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(dt => dt.ContractId)
            .HasColumnName("contract_id")
            .IsRequired();

        builder.Property(dt => dt.TransactionType)
            .HasColumnName("transaction_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(dt => dt.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(dt => dt.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.Property(dt => dt.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        builder.Property(dt => dt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(dt => dt.Contract)
            .WithMany(c => c.DepositTransactions)
            .HasForeignKey(dt => dt.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(dt => dt.ContractId)
            .HasDatabaseName("ix_deposit_transactions_contract_id");
    }
}
