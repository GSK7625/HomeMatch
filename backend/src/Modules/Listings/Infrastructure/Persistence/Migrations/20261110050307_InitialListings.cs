using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "listings");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "listings",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    landlord_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    deposit_amount = table.Column<long>(type: "bigint", nullable: false),
                    min_lease_months = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    house_rules = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    reviewed_by = table.Column<long>(type: "bigint", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    view_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.id);
                    table.CheckConstraint("ck_listings_deposit", "deposit_amount >= 0");
                    table.CheckConstraint("ck_listings_min_lease", "min_lease_months > 0");
                    table.CheckConstraint("ck_listings_price", "price > 0");
                    table.CheckConstraint("ck_listings_rejection", "status <> 'REJECTED' OR (rejection_reason IS NOT NULL AND length(btrim(rejection_reason)) > 0)");
                    table.CheckConstraint("ck_listings_revision", "revision > 0");
                    table.CheckConstraint("ck_listings_status", "status IN ('DRAFT', 'PENDING_APPROVAL', 'REJECTED', 'PUBLISHED', 'HIDDEN', 'EXPIRED')");
                    table.CheckConstraint("ck_listings_view_count", "view_count >= 0");
                });

            migrationBuilder.CreateTable(
                name: "listing_images",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    listing_id = table.Column<long>(type: "bigint", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    media_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "IMAGE"),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_images", x => x.id);
                    table.CheckConstraint("ck_listingimages_duration", "duration_seconds IS NULL OR duration_seconds > 0");
                    table.CheckConstraint("ck_listingimages_mediatype", "media_type IN ('IMAGE', 'VIDEO', 'VIRTUAL_TOUR_360')");
                    table.ForeignKey(
                        name: "FK_listing_images_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_locations",
                schema: "listings",
                columns: table => new
                {
                    listing_id = table.Column<long>(type: "bigint", nullable: false),
                    coordinates = table.Column<Point>(type: "geography(Point,4326)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_locations", x => x.listing_id);
                    table.ForeignKey(
                        name: "FK_listing_locations_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_views",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    listing_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_views", x => x.id);
                    table.ForeignKey(
                        name: "FK_listing_views_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_listings",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    listing_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_listings", x => x.id);
                    table.ForeignKey(
                        name: "FK_saved_listings_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_room_comparisons",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    listing_id = table.Column<long>(type: "bigint", nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_room_comparisons", x => x.id);
                    table.CheckConstraint("ck_comparisons_slot", "slot BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_user_room_comparisons_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_listing_images_listing_id",
                schema: "listings",
                table: "listing_images",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ix_listing_locations_coordinates",
                schema: "listings",
                table: "listing_locations",
                column: "coordinates")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_listing_views_listing_date",
                schema: "listings",
                table: "listing_views",
                columns: new[] { "listing_id", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_landlord_id",
                schema: "listings",
                table: "listings",
                column: "landlord_id");

            migrationBuilder.CreateIndex(
                name: "ix_listings_pending_review",
                schema: "listings",
                table: "listings",
                columns: new[] { "updated_at", "id" },
                filter: "status = 'PENDING_APPROVAL'");

            migrationBuilder.CreateIndex(
                name: "ux_listings_published_room",
                schema: "listings",
                table: "listings",
                column: "room_id",
                unique: true,
                filter: "status = 'PUBLISHED'");

            migrationBuilder.CreateIndex(
                name: "ux_listings_slug",
                schema: "listings",
                table: "listings",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_listings_listing_id",
                schema: "listings",
                table: "saved_listings",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ux_savedlistings_user_listing",
                schema: "listings",
                table: "saved_listings",
                columns: new[] { "user_id", "listing_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_room_comparisons_listing_id",
                schema: "listings",
                table: "user_room_comparisons",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ux_comparisons_user_listing",
                schema: "listings",
                table: "user_room_comparisons",
                columns: new[] { "user_id", "listing_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_comparisons_user_slot",
                schema: "listings",
                table: "user_room_comparisons",
                columns: new[] { "user_id", "slot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_images",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_locations",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_views",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "saved_listings",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "user_room_comparisons",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listings",
                schema: "listings");
        }
    }
}
