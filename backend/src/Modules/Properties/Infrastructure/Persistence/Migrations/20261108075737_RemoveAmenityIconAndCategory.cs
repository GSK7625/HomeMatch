using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAmenityIconAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_amenities_category",
                schema: "properties",
                table: "amenities");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "properties",
                table: "amenities");

            migrationBuilder.DropColumn(
                name: "icon_url",
                schema: "properties",
                table: "amenities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                schema: "properties",
                table: "amenities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "icon_url",
                schema: "properties",
                table: "amenities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_amenities_category",
                schema: "properties",
                table: "amenities",
                sql: "category IN ('ROOM', 'BUILDING', 'SECURITY', 'SERVICE')");
        }
    }
}
