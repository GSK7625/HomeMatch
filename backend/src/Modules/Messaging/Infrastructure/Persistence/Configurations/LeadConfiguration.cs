using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Messaging.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads", t =>
        {
            t.HasCheckConstraint("ck_leads_status", "status IN ('NEW', 'CONTACTED', 'VIEWING_SCHEDULED', 'CONTRACT_SIGNED', 'LOST')");
            t.HasCheckConstraint("ck_leads_source", "source IN ('DIRECT', 'SEARCH_FILTER', 'COMPARISON', 'AI_CHAT')");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(l => l.ListingId)
            .HasColumnName("listing_id")
            .IsRequired();

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(LeadStatus.New)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(l => l.Source)
            .HasColumnName("source")
            .HasMaxLength(50)
            .HasDefaultValue(LeadSource.Direct)
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(l => new { l.TenantId, l.ListingId })
            .IsUnique()
            .HasDatabaseName("uq_leads_tenant_listing");
    }
}
