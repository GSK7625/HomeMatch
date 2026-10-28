using HomeMatch.Modules.Listings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence;

public sealed class ListingsDbContext(DbContextOptions<ListingsDbContext> options) : DbContext(options)
{
    /// <summary>Bảng quản lý tin đăng cho thuê phòng trọ trên sàn</summary>
    public DbSet<Listing> Listings => Set<Listing>();

    /// <summary>Bảng quản lý album ảnh của bài đăng cho thuê</summary>
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    /// <summary>Bảng quản lý tọa độ không gian PostGIS phục vụ tìm kiếm theo bán kính</summary>
    public DbSet<ListingLocation> ListingLocations => Set<ListingLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Listing>(listing =>
        {
            listing.ToTable("listings", "listings", table =>
            {
                table.HasCheckConstraint("ck_listings_price", "price > 0");
                table.HasCheckConstraint("ck_listings_deposit", "deposit_amount >= 0");
                table.HasCheckConstraint("ck_listings_min_lease", "min_lease_months > 0");
                table.HasCheckConstraint("ck_listings_view_count", "view_count >= 0");
                table.HasCheckConstraint("ck_listings_status", "status IN ('DRAFT', 'PUBLISHED', 'HIDDEN', 'EXPIRED')");
            });
            listing.HasKey(x => x.Id);
            listing.Property(x => x.Title).HasMaxLength(255).IsRequired();
            listing.Property(x => x.Slug).HasMaxLength(255).IsRequired();
            listing.Property(x => x.Description).HasColumnType("text").IsRequired();
            listing.Property(x => x.MinLeaseMonths).HasDefaultValue(6);
            listing.Property(x => x.AllowPets).HasDefaultValue(false);
            listing.Property(x => x.HasCurfew).HasDefaultValue(false);
            listing.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT").IsRequired();
            listing.Property(x => x.ViewCount).HasDefaultValue(0L);
            listing.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            listing.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            listing.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ux_listings_slug");
            listing.HasIndex(x => x.RoomId).IsUnique().HasFilter("status = 'PUBLISHED'")
                .HasDatabaseName("ux_listings_published_room");
            listing.HasIndex(x => x.LandlordId).HasDatabaseName("ix_listings_landlord_id");
        });

        modelBuilder.Entity<ListingImage>(image =>
        {
            image.ToTable("listing_images", "listings");
            image.HasKey(x => x.Id);
            image.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            image.Property(x => x.IsPrimary).HasDefaultValue(false);
            image.Property(x => x.SortOrder).HasDefaultValue(0);
            image.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            image.HasIndex(x => x.ListingId).HasDatabaseName("ix_listing_images_listing_id");
            image.HasOne(x => x.Listing).WithMany(x => x.Images).HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ListingLocation>(location =>
        {
            location.ToTable("listing_locations", "listings");
            location.HasKey(x => x.ListingId);
            location.Property(x => x.Coordinates).HasColumnType("geography(Point,4326)");
            location.HasIndex(x => x.Coordinates).HasMethod("gist").HasDatabaseName("ix_listing_locations_coordinates");
            location.HasOne(x => x.Listing).WithOne().HasForeignKey<ListingLocation>(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
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
