using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "properties");

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    landlord_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    province_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ward_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address_detail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    property_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.id);
                    table.CheckConstraint("ck_properties_type", "property_type IN ('BOARDING_HOUSE', 'APARTMENT', 'MINI_APARTMENT', 'WHOLE_HOUSE')");
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false),
                    room_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    floor = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    area_sqm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    room_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_occupants = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    base_price = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "AVAILABLE"),
                    available_from = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                    table.CheckConstraint("ck_rooms_area", "area_sqm > 0");
                    table.CheckConstraint("ck_rooms_base_price", "base_price >= 0");
                    table.CheckConstraint("ck_rooms_max_occupants", "max_occupants > 0");
                    table.CheckConstraint("ck_rooms_status", "status IN ('AVAILABLE', 'RENTED', 'SUSPENDED')");
                    table.CheckConstraint("ck_rooms_type", "room_type IN ('STUDIO', '1PN', '2PN', 'DUPLEX', 'SHARED')");
                    table.ForeignKey(
                        name: "FK_rooms_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_properties_landlord_id",
                schema: "properties",
                table: "properties",
                column: "landlord_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_available_by_property",
                schema: "properties",
                table: "rooms",
                column: "property_id",
                filter: "status = 'AVAILABLE'");

            migrationBuilder.CreateIndex(
                name: "ux_rooms_property_number",
                schema: "properties",
                table: "rooms",
                columns: new[] { "property_id", "room_number" },
                unique: true);

            // Cross-module references use IDs only; no module accesses another module's entity or DbContext.
            migrationBuilder.AddForeignKey(
                name: "fk_properties_landlord_user",
                schema: "properties",
                table: "properties",
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
                name: "rooms",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "properties");
        }
    }
}
