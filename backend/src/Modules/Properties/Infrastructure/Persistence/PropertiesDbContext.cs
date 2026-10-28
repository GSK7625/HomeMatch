using HomeMatch.Modules.Properties.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class PropertiesDbContext(DbContextOptions<PropertiesDbContext> options) : DbContext(options)
{
    /// <summary>Bảng quản lý thông tin Tòa nhà / Bất động sản cho thuê</summary>
    public DbSet<Property> Properties => Set<Property>();

    /// <summary>Bảng quản lý các Phòng trọ chi tiết trực thuộc Tòa nhà</summary>
    public DbSet<Room> Rooms => Set<Room>();

    /// <summary>Danh mục Tỉnh / Thành phố trực thuộc Trung ương</summary>
    public DbSet<AdministrativeProvince> AdministrativeProvinces => Set<AdministrativeProvince>();

    /// <summary>Danh mục Phường / Xã trực thuộc Tỉnh/Thành phố</summary>
    public DbSet<AdministrativeWard> AdministrativeWards => Set<AdministrativeWard>();

    /// <summary>Danh mục Tiện ích cơ sở vật chất (Điều hòa, Nóng lạnh, Máy giặt, Ban công...)</summary>
    public DbSet<Amenity> Amenities => Set<Amenity>();

    /// <summary>Bảng liên kết Nhiều - Nhiều giữa Phòng và các Tiện ích có sẵn</summary>
    public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();

    /// <summary>Biểu giá dịch vụ điện nước của Tòa nhà (Giá điện/kWh, Giá nước/m3...)</summary>
    public DbSet<UtilityTariff> UtilityTariffs => Set<UtilityTariff>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdministrativeProvince>(province =>
        {
            province.ToTable("administrative_provinces", "properties");
            province.HasKey(x => x.Code);
            province.Property(x => x.Code).HasMaxLength(20);
            province.Property(x => x.Name).HasMaxLength(100).IsRequired();
            province.Property(x => x.NameEn).HasMaxLength(100);
            province.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<AdministrativeWard>(ward =>
        {
            ward.ToTable("administrative_wards", "properties");
            ward.HasKey(x => x.Code);
            ward.HasAlternateKey(x => new { x.ProvinceCode, x.Code });
            ward.Property(x => x.Code).HasMaxLength(20);
            ward.Property(x => x.Name).HasMaxLength(100).IsRequired();
            ward.Property(x => x.NameEn).HasMaxLength(100);
            ward.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            ward.Property(x => x.DistrictName).HasMaxLength(100);
            ward.Property(x => x.ProvinceCode).HasMaxLength(20).IsRequired();
            ward.HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.ProvinceCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Property>(property =>
        {
            property.ToTable("properties", "properties", table => table.HasCheckConstraint(
                "ck_properties_type", "property_type IN ('BOARDING_HOUSE', 'APARTMENT', 'MINI_APARTMENT', 'WHOLE_HOUSE')"));
            property.HasKey(x => x.Id);
            property.Property(x => x.Name).HasMaxLength(200).IsRequired();
            property.Property(x => x.ProvinceCode).HasMaxLength(20).IsRequired();
            property.Property(x => x.WardCode).HasMaxLength(20).IsRequired();
            property.Property(x => x.AddressDetail).HasMaxLength(255).IsRequired();
            property.Property(x => x.PropertyType).HasMaxLength(50).IsRequired();
            property.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            property.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            property.HasIndex(x => x.LandlordId).HasDatabaseName("ix_properties_landlord_id");
            property.HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.ProvinceCode)
                .OnDelete(DeleteBehavior.Restrict);
            property.HasOne<AdministrativeWard>().WithMany()
                .HasForeignKey(x => new { x.ProvinceCode, x.WardCode })
                .HasPrincipalKey(x => new { x.ProvinceCode, x.Code })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Room>(room =>
        {
            room.ToTable("rooms", "properties", table =>
            {
                table.HasCheckConstraint("ck_rooms_area", "area_sqm > 0");
                table.HasCheckConstraint("ck_rooms_max_occupants", "max_occupants > 0");
                table.HasCheckConstraint("ck_rooms_base_price", "base_price >= 0");
                table.HasCheckConstraint("ck_rooms_status", "status IN ('AVAILABLE', 'RENTED', 'SUSPENDED')");
                table.HasCheckConstraint("ck_rooms_type", "room_type IN ('STUDIO', '1PN', '2PN', 'DUPLEX', 'SHARED')");
            });
            room.HasKey(x => x.Id);
            room.HasAlternateKey(x => new { x.PropertyId, x.Id })
                .HasName("ak_rooms_property_id_id");
            room.Property(x => x.RoomNumber).HasMaxLength(50).IsRequired();
            room.Property(x => x.Floor).HasDefaultValue(1);
            room.Property(x => x.AreaSqm).HasPrecision(6, 2);
            room.Property(x => x.RoomType).HasMaxLength(50).IsRequired();
            room.Property(x => x.MaxOccupants).HasDefaultValue(2);
            room.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("AVAILABLE").IsRequired();
            room.Property(x => x.AvailableFrom).HasDefaultValueSql("CURRENT_DATE");
            room.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            room.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            room.HasIndex(x => new { x.PropertyId, x.RoomNumber }).IsUnique()
                .HasDatabaseName("ux_rooms_property_number");
            room.HasIndex(x => x.PropertyId).HasFilter("status = 'AVAILABLE'")
                .HasDatabaseName("ix_rooms_available_by_property");
            room.HasOne(x => x.Property).WithMany(x => x.Rooms).HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Amenity>(amenity =>
        {
            amenity.ToTable("amenities", "properties", table => table.HasCheckConstraint(
                "ck_amenities_category", "category IN ('ROOM', 'BUILDING', 'SECURITY', 'SERVICE')"));
            amenity.HasKey(x => x.Id);
            amenity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            amenity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            amenity.Property(x => x.IconUrl).HasMaxLength(500);
            amenity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            amenity.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_amenities_name");
            amenity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_amenities_code");
        });

        modelBuilder.Entity<RoomAmenity>(roomAmenity =>
        {
            roomAmenity.ToTable("room_amenities", "properties");
            roomAmenity.HasKey(x => new { x.RoomId, x.AmenityId });
            roomAmenity.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            roomAmenity.HasOne<Amenity>().WithMany().HasForeignKey(x => x.AmenityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UtilityTariff>(tariff =>
        {
            tariff.ToTable("utility_tariffs", "properties", table =>
            {
                table.HasCheckConstraint("ck_utility_tariffs_type",
                    "tariff_type IN ('ELECTRICITY', 'WATER', 'INTERNET', 'SERVICE', 'CLEANING')");
                table.HasCheckConstraint("ck_utility_tariffs_calc_method",
                    "calc_method IN ('PER_KWH', 'PER_M3', 'PER_PERSON', 'FIXED_ROOM')");
                table.HasCheckConstraint("ck_utility_tariffs_price", "unit_price >= 0");
            });
            tariff.HasKey(x => x.Id);
            tariff.Property(x => x.TariffType).HasMaxLength(50).IsRequired();
            tariff.Property(x => x.CalcMethod).HasMaxLength(20).IsRequired();
            tariff.Property(x => x.EffectiveFrom).HasDefaultValueSql("CURRENT_DATE");
            tariff.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            tariff.HasOne<Room>().WithMany()
                .HasForeignKey(x => new { x.PropertyId, x.RoomId })
                .HasPrincipalKey(x => new { x.PropertyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            tariff.HasIndex(x => new { x.PropertyId, x.RoomId, x.TariffType, x.EffectiveFrom })
                .HasDatabaseName("ix_utility_tariffs_scope_type_date");
            tariff.HasIndex(x => new { x.PropertyId, x.TariffType, x.EffectiveFrom })
                .IsUnique().HasFilter("room_id IS NULL")
                .HasDatabaseName("ux_utility_tariffs_property_type_date");
            tariff.HasIndex(x => new { x.RoomId, x.TariffType, x.EffectiveFrom })
                .IsUnique().HasFilter("room_id IS NOT NULL")
                .HasDatabaseName("ux_utility_tariffs_room_type_date");
        });

        UseSnakeCaseColumns(modelBuilder);
    }

    private static void UseSnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(string.Concat(property.Name.Select((c, i) =>
                char.IsUpper(c) && i > 0 ? $"_{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString())));
    }
}
