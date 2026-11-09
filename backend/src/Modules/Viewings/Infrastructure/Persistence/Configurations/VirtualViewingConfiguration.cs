using HomeMatch.Modules.Viewings.Domain.Entities;
using HomeMatch.Modules.Viewings.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Viewings.Infrastructure.Persistence.Configurations;

public class VirtualViewingConfiguration : IEntityTypeConfiguration<VirtualViewing>
{
    public void Configure(EntityTypeBuilder<VirtualViewing> builder)
    {
        builder.ToTable("virtual_viewings", t =>
        {
            t.HasCheckConstraint("ck_virtualviewings_status", "status IN ('SCHEDULED', 'IN_PROGRESS', 'ENDED', 'CANCELLED')");
        });

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.AppointmentId)
            .HasColumnName("appointment_id")
            .IsRequired();

        builder.Property(v => v.RoomUrl)
            .HasColumnName("room_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(VirtualViewingStatus.Scheduled)
            .IsRequired();

        builder.Property(v => v.StartedAt)
            .HasColumnName("started_at");

        builder.Property(v => v.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(v => v.AppointmentId)
            .IsUnique()
            .HasDatabaseName("uq_virtualviewings_app");

        builder.HasOne(v => v.Appointment)
            .WithOne(a => a.VirtualViewing)
            .HasForeignKey<VirtualViewing>(v => v.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
