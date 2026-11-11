using HomeMatch.Modules.Viewings.Domain.Entities;
using HomeMatch.Modules.Viewings.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Viewings.Infrastructure.Persistence.Configurations;

public class ViewingAppointmentConfiguration : IEntityTypeConfiguration<ViewingAppointment>
{
    public void Configure(EntityTypeBuilder<ViewingAppointment> builder)
    {
        builder.ToTable("viewing_appointments", t =>
        {
            t.HasCheckConstraint("ck_appointments_duration", "duration_minutes BETWEEN 15 AND 180");
            t.HasCheckConstraint("ck_appointments_status", "status IN ('PENDING', 'CONFIRMED', 'CANCELLED', 'COMPLETED')");
            t.HasCheckConstraint("ck_appointments_cancelled_by", "cancelled_by IS NULL OR cancelled_by IN ('TENANT', 'LANDLORD', 'SYSTEM')");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(a => a.ListingId)
            .HasColumnName("listing_id")
            .IsRequired();

        builder.Property(a => a.AppointmentTime)
            .HasColumnName("appointment_time")
            .IsRequired();

        builder.Property(a => a.DurationMinutes)
            .HasColumnName("duration_minutes")
            .HasDefaultValue(30)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(ViewingAppointmentStatus.Pending)
            .IsRequired();

        builder.Property(a => a.CancelledBy)
            .HasColumnName("cancelled_by")
            .HasMaxLength(20);

        builder.Property(a => a.CancelledReason)
            .HasColumnName("cancelled_reason")
            .HasMaxLength(500);

        builder.Property(a => a.RemindAt)
            .HasColumnName("remind_at");

        builder.Property(a => a.IsReminded)
            .HasColumnName("is_reminded")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("ix_viewing_appointments_tenant_id");

        builder.HasIndex(a => a.ListingId)
            .HasDatabaseName("ix_viewing_appointments_listing_id");

        builder.HasIndex(a => new { a.ListingId, a.AppointmentTime })
            .HasDatabaseName("ix_viewing_appointments_listing_time");
    }
}
