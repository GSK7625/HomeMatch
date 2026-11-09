using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteListingsFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ownership and room validity are checked through Shared contracts, not cross-schema foreign keys.
            migrationBuilder.DropForeignKey("fk_listings_room", "listings", "listings");
            migrationBuilder.DropForeignKey("fk_listings_landlord_user", "listings", "listings");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                schema: "listings",
                table: "listings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "duration_seconds",
                schema: "listings",
                table: "listing_images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "media_type",
                schema: "listings",
                table: "listing_images",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "IMAGE");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                schema: "listings",
                table: "listing_images",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_listingimages_duration",
                schema: "listings",
                table: "listing_images",
                sql: "duration_seconds IS NULL OR duration_seconds > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_listingimages_mediatype",
                schema: "listings",
                table: "listing_images",
                sql: "media_type IN ('IMAGE', 'VIDEO', 'VIRTUAL_TOUR_360')");

            migrationBuilder.CreateIndex(
                name: "ix_listing_views_listing_date",
                schema: "listings",
                table: "listing_views",
                columns: new[] { "listing_id", "viewed_at" });

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
            migrationBuilder.AddForeignKey(
                name: "fk_listings_room", schema: "listings", table: "listings", column: "room_id",
                principalSchema: "properties", principalTable: "rooms", principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "fk_listings_landlord_user", schema: "listings", table: "listings", column: "landlord_id",
                principalSchema: "identity", principalTable: "users", principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "listing_views",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "saved_listings",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "user_room_comparisons",
                schema: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listingimages_duration",
                schema: "listings",
                table: "listing_images");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listingimages_mediatype",
                schema: "listings",
                table: "listing_images");

            migrationBuilder.DropColumn(
                name: "is_featured",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "duration_seconds",
                schema: "listings",
                table: "listing_images");

            migrationBuilder.DropColumn(
                name: "media_type",
                schema: "listings",
                table: "listing_images");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                schema: "listings",
                table: "listing_images");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                schema: "listings",
                table: "listings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);
        }
    }
}
