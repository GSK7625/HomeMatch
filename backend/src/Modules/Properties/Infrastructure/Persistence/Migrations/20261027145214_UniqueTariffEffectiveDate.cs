using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueTariffEffectiveDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_utility_tariffs_room_id",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.CreateIndex(
                name: "ux_utility_tariffs_property_type_date",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "tariff_type", "effective_from" },
                unique: true,
                filter: "room_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_utility_tariffs_room_type_date",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "room_id", "tariff_type", "effective_from" },
                unique: true,
                filter: "room_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_utility_tariffs_property_type_date",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropIndex(
                name: "ux_utility_tariffs_room_type_date",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.CreateIndex(
                name: "IX_utility_tariffs_room_id",
                schema: "properties",
                table: "utility_tariffs",
                column: "room_id");
        }
    }
}
