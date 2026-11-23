using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCatalogsAndRoomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_rooms_type",
                schema: "properties",
                table: "rooms");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "properties",
                table: "amenities",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "room_types",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_room_types_code",
                schema: "properties",
                table: "room_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_room_types_name",
                schema: "properties",
                table: "room_types",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_types",
                schema: "properties");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "properties",
                table: "amenities");

            migrationBuilder.AddCheckConstraint(
                name: "ck_rooms_type",
                schema: "properties",
                table: "rooms",
                sql: "room_type IN ('SINGLE', 'STUDIO', '1B1B', '2B1B', 'DORMITORY')");
        }
    }
}
