using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PropertyReferenceDataAndTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.id);
                    table.CheckConstraint("ck_amenities_category", "category IN ('ROOM', 'BUILDING', 'SECURITY', 'SERVICE')");
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
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utility_tariffs", x => x.id);
                    table.CheckConstraint("ck_utility_tariffs_calc_method", "calc_method IN ('PER_KWH', 'PER_M3', 'PER_PERSON', 'FIXED_ROOM')");
                    table.CheckConstraint("ck_utility_tariffs_price", "unit_price >= 0");
                    table.CheckConstraint("ck_utility_tariffs_type", "tariff_type IN ('ELECTRICITY', 'WATER', 'INTERNET', 'SERVICE', 'CLEANING')");
                    table.ForeignKey(
                        name: "FK_utility_tariffs_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_utility_tariffs_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "properties",
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_properties_province_code_ward_code",
                schema: "properties",
                table: "properties",
                columns: new[] { "province_code", "ward_code" });

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
                name: "IX_room_amenities_amenity_id",
                schema: "properties",
                table: "room_amenities",
                column: "amenity_id");

            migrationBuilder.CreateIndex(
                name: "IX_utility_tariffs_room_id",
                schema: "properties",
                table: "utility_tariffs",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_utility_tariffs_scope_type_date",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "room_id", "tariff_type", "effective_from" });

            migrationBuilder.AddForeignKey(
                name: "FK_properties_administrative_provinces_province_code",
                schema: "properties",
                table: "properties",
                column: "province_code",
                principalSchema: "properties",
                principalTable: "administrative_provinces",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_properties_administrative_wards_province_code_ward_code",
                schema: "properties",
                table: "properties",
                columns: new[] { "province_code", "ward_code" },
                principalSchema: "properties",
                principalTable: "administrative_wards",
                principalColumns: new[] { "province_code", "code" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_properties_administrative_provinces_province_code",
                schema: "properties",
                table: "properties");

            migrationBuilder.DropForeignKey(
                name: "FK_properties_administrative_wards_province_code_ward_code",
                schema: "properties",
                table: "properties");

            migrationBuilder.DropTable(
                name: "administrative_wards",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "room_amenities",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "utility_tariffs",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "administrative_provinces",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "amenities",
                schema: "properties");

            migrationBuilder.DropIndex(
                name: "IX_properties_province_code_ward_code",
                schema: "properties",
                table: "properties");
        }
    }
}
