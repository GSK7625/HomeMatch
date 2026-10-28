using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TariffRoomScopeIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_utility_tariffs_rooms_room_id",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_rooms_property_id_id",
                schema: "properties",
                table: "rooms",
                columns: new[] { "property_id", "id" });

            migrationBuilder.AddForeignKey(
                name: "FK_utility_tariffs_rooms_property_id_room_id",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "room_id" },
                principalSchema: "properties",
                principalTable: "rooms",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_utility_tariffs_rooms_property_id_room_id",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_rooms_property_id_id",
                schema: "properties",
                table: "rooms");

            migrationBuilder.AddForeignKey(
                name: "FK_utility_tariffs_rooms_room_id",
                schema: "properties",
                table: "utility_tariffs",
                column: "room_id",
                principalSchema: "properties",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
