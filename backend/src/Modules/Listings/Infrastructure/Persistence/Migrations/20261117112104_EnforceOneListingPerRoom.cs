using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneListingPerRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_listings_published_room",
                schema: "listings",
                table: "listings");

            migrationBuilder.CreateIndex(
                name: "ux_listings_room",
                schema: "listings",
                table: "listings",
                column: "room_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_listings_room",
                schema: "listings",
                table: "listings");

            migrationBuilder.CreateIndex(
                name: "ux_listings_published_room",
                schema: "listings",
                table: "listings",
                column: "room_id",
                unique: true,
                filter: "status = 'PUBLISHED'");
        }
    }
}
