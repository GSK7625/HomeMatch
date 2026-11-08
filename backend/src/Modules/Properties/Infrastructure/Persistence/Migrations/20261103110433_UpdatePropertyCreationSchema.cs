using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePropertyCreationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_utility_tariffs_scope_type_date",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropIndex(
                name: "ux_utility_tariffs_property_type_date",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropIndex(
                name: "ux_utility_tariffs_room_type_date",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_utility_tariffs_calc_method",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_utility_tariffs_type",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rooms_type",
                schema: "properties",
                table: "rooms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_properties_type",
                schema: "properties",
                table: "properties");

            migrationBuilder.Sql("""
                UPDATE properties.utility_tariffs
                SET calc_method = 'FIXED_PER_MONTH'
                WHERE calc_method = 'FIXED_ROOM';
                """);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "properties",
                table: "utility_tariffs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                schema: "properties",
                table: "properties",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                schema: "properties",
                table: "properties",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'postgis') THEN
                        CREATE EXTENSION IF NOT EXISTS postgis;
                        ALTER TABLE properties.properties ADD COLUMN geom geometry(Point, 4326)
                            GENERATED ALWAYS AS (
                                CASE WHEN latitude IS NOT NULL AND longitude IS NOT NULL
                                THEN ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)
                                ELSE NULL END) STORED;
                        CREATE INDEX ix_properties_geom ON properties.properties USING GIST (geom);
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_tariffs_scope",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "room_id", "tariff_type" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_utility_tariffs_calc_method",
                schema: "properties",
                table: "utility_tariffs",
                sql: "calc_method IN ('PER_KWH', 'PER_M3', 'PER_PERSON', 'FIXED_PER_MONTH', 'FREE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_utility_tariffs_type",
                schema: "properties",
                table: "utility_tariffs",
                sql: "tariff_type IN ('ELECTRICITY', 'WATER', 'INTERNET', 'SERVICE', 'CLEANING', 'PARKING')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_rooms_type",
                schema: "properties",
                table: "rooms",
                sql: "room_type IN ('SINGLE', 'STUDIO', '1B1B', '2B1B', 'DORMITORY')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_properties_type",
                schema: "properties",
                table: "properties",
                sql: "property_type IN ('APARTMENT', 'BOARDING_HOUSE', 'SERVICED_APARTMENT', 'HOMESTAY')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_tariffs_scope",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_utility_tariffs_calc_method",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_utility_tariffs_type",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rooms_type",
                schema: "properties",
                table: "rooms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_properties_type",
                schema: "properties",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "properties",
                table: "utility_tariffs");

            migrationBuilder.Sql("ALTER TABLE properties.properties DROP COLUMN IF EXISTS geom;");

            migrationBuilder.DropColumn(
                name: "latitude",
                schema: "properties",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "longitude",
                schema: "properties",
                table: "properties");

            migrationBuilder.CreateIndex(
                name: "ix_utility_tariffs_scope_type_date",
                schema: "properties",
                table: "utility_tariffs",
                columns: new[] { "property_id", "room_id", "tariff_type", "effective_from" });

            migrationBuilder.Sql("""
                UPDATE properties.utility_tariffs
                SET calc_method = 'FIXED_ROOM'
                WHERE calc_method = 'FIXED_PER_MONTH';
                """);

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_utility_tariffs_calc_method",
                schema: "properties",
                table: "utility_tariffs",
                sql: "calc_method IN ('PER_KWH', 'PER_M3', 'PER_PERSON', 'FIXED_ROOM')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_utility_tariffs_type",
                schema: "properties",
                table: "utility_tariffs",
                sql: "tariff_type IN ('ELECTRICITY', 'WATER', 'INTERNET', 'SERVICE', 'CLEANING')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_rooms_type",
                schema: "properties",
                table: "rooms",
                sql: "room_type IN ('STUDIO', '1PN', '2PN', 'DUPLEX', 'SHARED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_properties_type",
                schema: "properties",
                table: "properties",
                sql: "property_type IN ('BOARDING_HOUSE', 'APARTMENT', 'MINI_APARTMENT', 'WHOLE_HOUSE')");
        }
    }
}
