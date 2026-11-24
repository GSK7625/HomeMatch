using HomeMatch.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Configurations;

public class UtilityReadingConfiguration : IEntityTypeConfiguration<UtilityReading>
{
    public void Configure(EntityTypeBuilder<UtilityReading> builder)
    {
        builder.ToTable("utility_readings", BillingDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_utility_readings_month", "billing_month BETWEEN 1 AND 12");
            table.HasCheckConstraint("ck_utility_readings_year", "billing_year >= 2020");
            table.HasCheckConstraint("ck_utility_readings_elec_old", "electricity_old >= 0");
            table.HasCheckConstraint("ck_utility_readings_elec_progress", "electricity_new >= electricity_old");
            table.HasCheckConstraint("ck_utility_readings_water_old", "water_old >= 0");
            table.HasCheckConstraint("ck_utility_readings_water_progress", "water_new >= water_old");
            table.HasCheckConstraint("ck_utility_readings_costs", "electricity_cost >= 0 AND water_cost >= 0 AND service_cost >= 0");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.PropertyId)
            .HasColumnName("property_id")
            .IsRequired();

        builder.Property(u => u.RoomId)
            .HasColumnName("room_id")
            .IsRequired();

        builder.Property(u => u.ContractId)
            .HasColumnName("contract_id")
            .IsRequired();

        builder.Property(u => u.LandlordId)
            .HasColumnName("landlord_id")
            .IsRequired();

        builder.Property(u => u.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(u => u.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        builder.Property(u => u.BillingMonth)
            .HasColumnName("billing_month")
            .IsRequired();

        builder.Property(u => u.BillingYear)
            .HasColumnName("billing_year")
            .IsRequired();

        builder.Property(u => u.ElectricityOld)
            .HasColumnName("electricity_old")
            .IsRequired();

        builder.Property(u => u.ElectricityNew)
            .HasColumnName("electricity_new")
            .IsRequired();

        builder.Property(u => u.ElectricityUsage)
            .HasColumnName("electricity_usage")
            .IsRequired();

        builder.Property(u => u.ElectricityUnitPrice)
            .HasColumnName("electricity_unit_price")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(u => u.ElectricityCost)
            .HasColumnName("electricity_cost")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(u => u.WaterOld)
            .HasColumnName("water_old")
            .IsRequired();

        builder.Property(u => u.WaterNew)
            .HasColumnName("water_new")
            .IsRequired();

        builder.Property(u => u.WaterUsage)
            .HasColumnName("water_usage")
            .IsRequired();

        builder.Property(u => u.WaterUnitPrice)
            .HasColumnName("water_unit_price")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(u => u.WaterCost)
            .HasColumnName("water_cost")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(u => u.ServiceCost)
            .HasColumnName("service_cost")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(u => u.ReadingDate)
            .HasColumnName("reading_date")
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(u => new { u.RoomId, u.BillingYear, u.BillingMonth })
            .IsUnique()
            .HasDatabaseName("uq_utility_readings_room_period");

        builder.HasIndex(u => new { u.ContractId, u.BillingYear, u.BillingMonth })
            .HasDatabaseName("ix_utility_readings_contract_period");

        builder.HasIndex(u => new { u.LandlordId, u.PropertyId, u.BillingYear, u.BillingMonth })
            .HasDatabaseName("ix_utility_readings_landlord_property");
    }
}
