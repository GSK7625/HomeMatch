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
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    deposit_amount = table.Column<long>(type: "bigint", nullable: false),
                    min_lease_months = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    allow_pets = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    has_curfew = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    curfew_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    view_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
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
                    table.CheckConstraint("ck_listings_status", "status IN ('DRAFT', 'PUBLISHED', 'HIDDEN', 'EXPIRED')");
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_images", x => x.id);
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
                name: "ix_listings_landlord_id",
                schema: "listings",
                table: "listings",
                column: "landlord_id");

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

            // Keep the database references without coupling module entity models.
            migrationBuilder.AddForeignKey(
                name: "fk_listings_room",
                schema: "listings",
                table: "listings",
                column: "room_id",
                principalSchema: "properties",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_landlord_user",
                schema: "listings",
                table: "listings",
                column: "landlord_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
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
                name: "listings",
                schema: "listings");
        }
    }
}
