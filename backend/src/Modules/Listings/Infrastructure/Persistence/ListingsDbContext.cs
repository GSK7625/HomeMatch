using HomeMatch.Modules.Listings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence;

public sealed class ListingsDbContext(DbContextOptions<ListingsDbContext> options) : DbContext(options)
{
    /// <summary>Bảng quản lý tin đăng cho thuê phòng trọ trên sàn</summary>
    public DbSet<Listing> Listings => Set<Listing>();

    /// <summary>Bảng quản lý album ảnh của bài đăng cho thuê</summary>
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    /// <summary>Bảng quản lý danh sách tin yêu thích</summary>
    public DbSet<SavedListing> SavedListings => Set<SavedListing>();

    /// <summary>Bảng quản lý nhật ký lượt xem tin đăng</summary>
    public DbSet<ListingView> ListingViews => Set<ListingView>();

    /// <summary>Bảng quản lý khay so sánh phòng</summary>
    public DbSet<UserRoomComparison> UserRoomComparisons => Set<UserRoomComparison>();

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
                table.HasCheckConstraint("ck_listings_status", "status IN ('DRAFT', 'PENDING_APPROVAL', 'REJECTED', 'PUBLISHED', 'HIDDEN', 'EXPIRED')");
                table.HasCheckConstraint("ck_listings_rejection", "status <> 'REJECTED' OR (rejection_reason IS NOT NULL AND length(btrim(rejection_reason)) > 0)");
                table.HasCheckConstraint("ck_listings_revision", "revision > 0");
            });
            listing.HasKey(x => x.Id);
            listing.Property(x => x.Title).HasMaxLength(255).IsRequired();
            listing.Property(x => x.Slug).HasMaxLength(300).IsRequired();
            listing.Property(x => x.Description).HasColumnType("text").IsRequired();
            listing.Property(x => x.MinLeaseMonths).HasDefaultValue(6);
            listing.Property(x => x.AllowPets).HasDefaultValue(false);
            listing.Property(x => x.HasCurfew).HasDefaultValue(false);
            listing.Property(x => x.IsFeatured).HasDefaultValue(false);
            listing.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT").IsRequired();
            listing.Property(x => x.ViewCount).HasDefaultValue(0L);
            listing.Property(x => x.Revision).HasDefaultValue(1L).IsConcurrencyToken();
            listing.Property(x => x.RejectionReason).HasMaxLength(500);
            listing.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            listing.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            listing.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ux_listings_slug");
            listing.HasIndex(x => x.RoomId).IsUnique().HasFilter("status = 'PUBLISHED'")
                .HasDatabaseName("ux_listings_published_room");
            listing.HasIndex(x => x.LandlordId).HasDatabaseName("ix_listings_landlord_id");
            listing.HasIndex(x => new { x.UpdatedAt, x.Id }).HasFilter("status = 'PENDING_APPROVAL'")
                .HasDatabaseName("ix_listings_pending_review");
        });

        modelBuilder.Entity<ListingImage>(image =>
        {
            image.ToTable("listing_images", "listings", table =>
            {
                table.HasCheckConstraint("ck_listingimages_mediatype", "media_type IN ('IMAGE', 'VIDEO', 'VIRTUAL_TOUR_360')");
                table.HasCheckConstraint("ck_listingimages_duration", "duration_seconds IS NULL OR duration_seconds > 0");
            });
            image.HasKey(x => x.Id);
            image.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            image.Property(x => x.IsPrimary).HasDefaultValue(false);
            image.Property(x => x.SortOrder).HasDefaultValue(0);
            image.Property(x => x.MediaType).HasMaxLength(20).HasDefaultValue("IMAGE").IsRequired();
            image.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            image.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            image.HasIndex(x => x.ListingId).HasDatabaseName("ix_listing_images_listing_id");
            image.HasOne(x => x.Listing).WithMany(x => x.Images).HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedListing>(saved =>
        {
            saved.ToTable("saved_listings", "listings");
            saved.HasKey(x => x.Id);
            saved.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            saved.HasIndex(x => new { x.UserId, x.ListingId }).IsUnique().HasDatabaseName("ux_savedlistings_user_listing");
            saved.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ListingView>(view =>
        {
            view.ToTable("listing_views", "listings");
            view.HasKey(x => x.Id);
            view.Property(x => x.IpAddress).HasMaxLength(45);
            view.Property(x => x.ViewedAt).HasDefaultValueSql("now()");
            view.HasIndex(x => new { x.ListingId, x.ViewedAt }).HasDatabaseName("ix_listing_views_listing_date");
            view.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRoomComparison>(comp =>
        {
            comp.ToTable("user_room_comparisons", "listings", table =>
                table.HasCheckConstraint("ck_comparisons_slot", "slot BETWEEN 1 AND 3"));
            comp.HasKey(x => x.Id);
            comp.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            comp.HasIndex(x => new { x.UserId, x.Slot }).IsUnique().HasDatabaseName("ux_comparisons_user_slot");
            comp.HasIndex(x => new { x.UserId, x.ListingId }).IsUnique().HasDatabaseName("ux_comparisons_user_listing");
            comp.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId)
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

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AdvanceRevisions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AdvanceRevisions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void AdvanceRevisions()
    {
        foreach (var entry in ChangeTracker.Entries<Listing>().Where(entry => entry.State == EntityState.Modified))
            entry.Entity.Revision = checked(entry.Property(item => item.Revision).OriginalValue + 1);
    }

    private static void UseSnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(string.Concat(property.Name.Select((c, i) =>
                char.IsUpper(c) && i > 0 ? $"_{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString())));
    }
}
