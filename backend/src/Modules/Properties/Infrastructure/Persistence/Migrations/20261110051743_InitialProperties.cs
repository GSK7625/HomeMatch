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
                name: "administrative_provinces",
                schema: "properties",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrative_provinces", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "amenities",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "administrative_wards",
                schema: "properties",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    district_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    province_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrative_wards", x => x.code);
                    table.UniqueConstraint("AK_administrative_wards_province_code_code", x => new { x.province_code, x.code });
                    table.ForeignKey(
                        name: "FK_administrative_wards_administrative_provinces_province_code",
                        column: x => x.province_code,
                        principalSchema: "properties",
                        principalTable: "administrative_provinces",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.id);
                    table.CheckConstraint("ck_properties_type", "property_type IN ('APARTMENT', 'BOARDING_HOUSE', 'SERVICED_APARTMENT', 'HOMESTAY')");
                    table.ForeignKey(
                        name: "FK_properties_administrative_provinces_province_code",
                        column: x => x.province_code,
                        principalSchema: "properties",
                        principalTable: "administrative_provinces",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_properties_administrative_wards_province_code_ward_code",
                        columns: x => new { x.province_code, x.ward_code },
                        principalSchema: "properties",
                        principalTable: "administrative_wards",
                        principalColumns: new[] { "province_code", "code" },
                        onDelete: ReferentialAction.Restrict);
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
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "AVAILABLE"),
                    available_from = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                    table.UniqueConstraint("ak_rooms_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_rooms_area", "area_sqm > 0");
                    table.CheckConstraint("ck_rooms_max_occupants", "max_occupants > 0");
                    table.CheckConstraint("ck_rooms_status", "status IN ('AVAILABLE', 'RENTED', 'SUSPENDED')");
                    table.CheckConstraint("ck_rooms_type", "room_type IN ('SINGLE', 'STUDIO', '1B1B', '2B1B', 'DORMITORY')");
                    table.ForeignKey(
                        name: "FK_rooms_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "room_amenities",
                schema: "properties",
                columns: table => new
                {
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    amenity_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_amenities", x => new { x.room_id, x.amenity_id });
                    table.ForeignKey(
                        name: "FK_room_amenities_amenities_amenity_id",
                        column: x => x.amenity_id,
                        principalSchema: "properties",
                        principalTable: "amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_amenities_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "properties",
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "utility_tariffs",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false),
                    room_id = table.Column<long>(type: "bigint", nullable: true),
                    tariff_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    calc_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utility_tariffs", x => x.id);
                    table.CheckConstraint("ck_utility_tariffs_calc_method", "calc_method IN ('PER_KWH', 'PER_M3', 'PER_PERSON', 'FIXED_PER_MONTH', 'FREE')");
                    table.CheckConstraint("ck_utility_tariffs_price", "unit_price >= 0");
                    table.CheckConstraint("ck_utility_tariffs_type", "tariff_type IN ('ELECTRICITY', 'WATER', 'INTERNET', 'SERVICE', 'CLEANING', 'PARKING')");
                    table.ForeignKey(
                        name: "FK_utility_tariffs_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_utility_tariffs_rooms_property_id_room_id",
                        columns: x => new { x.property_id, x.room_id },
                        principalSchema: "properties",
                        principalTable: "rooms",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_amenities_code",
                schema: "properties",
                table: "amenities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_amenities_name",
                schema: "properties",
                table: "amenities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_properties_landlord_id",
                schema: "properties",
                table: "properties",
                column: "landlord_id");

            migrationBuilder.CreateIndex(
                name: "IX_properties_province_code_ward_code",
                schema: "properties",
                table: "properties",
                columns: new[] { "province_code", "ward_code" });

            migrationBuilder.CreateIndex(
                name: "IX_room_amenities_amenity_id",
                schema: "properties",
                table: "room_amenities",
                column: "amenity_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_utility_tariffs_property_id_room_id",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ux_utility_tariffs_property_type",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "tariff_type" },
                unique: true,
                filter: "room_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_utility_tariffs_room_type",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "room_id", "tariff_type" },
                unique: true,
                filter: "room_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_amenities",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "utility_tariffs",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "amenities",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "rooms",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "administrative_wards",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "administrative_provinces",
                schema: "properties");
        }
    }
}
