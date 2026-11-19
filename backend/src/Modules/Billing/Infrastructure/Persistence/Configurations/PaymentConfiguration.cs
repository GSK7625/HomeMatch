using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", BillingDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_payments_status", "status IN ('PENDING', 'SUCCESS', 'FAILED', 'EXPIRED')");
            table.HasCheckConstraint("ck_payments_amount", "amount > 0");
            table.HasCheckConstraint("ck_payments_method", "payment_method IN ('VNPAY', 'BANK_TRANSFER', 'CASH', 'MOMO')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.PaymentCode)
            .HasColumnName("payment_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasColumnName("payment_method")
            .HasMaxLength(20)
            .HasDefaultValue(PaymentMethod.VNPay)
            .IsRequired();

        builder.Property(p => p.GatewayTransactionNo)
            .HasColumnName("gateway_transaction_no")
            .HasMaxLength(100);

        builder.Property(p => p.BankCode)
            .HasColumnName("bank_code")
            .HasMaxLength(20);

        builder.Property(p => p.CardType)
            .HasColumnName("card_type")
            .HasMaxLength(20);

        builder.Property(p => p.GatewayResponseCode)
            .HasColumnName("gateway_response_code")
            .HasMaxLength(10);

        builder.Property(p => p.PaymentUrl)
            .HasColumnName("payment_url");

        builder.Property(p => p.ProofImageUrl)
            .HasColumnName("proof_image_url")
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Pending)
            .IsRequired();

        builder.Property(p => p.ExpiredAt)
            .HasColumnName("expired_at")
            .IsRequired();

        builder.Property(p => p.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(p => p.RawIpnData)
            .HasColumnName("raw_ipn_data");

        builder.Property(p => p.VerifiedBy)
            .HasColumnName("verified_by");

        builder.Property(p => p.VerifiedAt)
            .HasColumnName("verified_at");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(p => p.PaymentCode)
            .IsUnique()
            .HasDatabaseName("uq_payments_code");

        // Giới hạn duy nhất 1 giao dịch PENDING cho mỗi hóa đơn (BR-BIL-22)
        builder.HasIndex(p => p.InvoiceId)
            .IsUnique()
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("uq_payments_invoice_single_pending");

        builder.HasIndex(p => new { p.InvoiceId, p.Status })
            .HasDatabaseName("ix_payments_invoice_status");

        // Relationship
        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
