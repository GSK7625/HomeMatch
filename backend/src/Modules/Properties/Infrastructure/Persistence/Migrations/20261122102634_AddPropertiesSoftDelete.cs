using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertiesSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rooms_property_number",
                schema: "properties",
                table: "rooms");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "properties",
                table: "rooms",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deletion_requested_at",
                schema: "properties",
                table: "rooms",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "properties",
                table: "properties",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_rooms_property_number",
                schema: "properties",
                table: "rooms",
                columns: new[] { "property_id", "room_number" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rooms_property_number",
                schema: "properties",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "properties",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "deletion_requested_at",
                schema: "properties",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "properties",
                table: "properties");

            migrationBuilder.CreateIndex(
                name: "ux_rooms_property_number",
                schema: "properties",
                table: "rooms",
                columns: new[] { "property_id", "room_number" },
                unique: true);
        }
    }
}
