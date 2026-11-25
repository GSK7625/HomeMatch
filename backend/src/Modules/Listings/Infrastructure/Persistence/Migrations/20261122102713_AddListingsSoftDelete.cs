using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingsSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_listings_room",
                schema: "listings",
                table: "listings");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "listings",
                table: "listings",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_listings_room",
                schema: "listings",
                table: "listings",
                column: "room_id",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_listings_room",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "listings",
                table: "listings");

            migrationBuilder.CreateIndex(
                name: "ux_listings_room",
                schema: "listings",
                table: "listings",
                column: "room_id",
                unique: true);
        }
    }
}
