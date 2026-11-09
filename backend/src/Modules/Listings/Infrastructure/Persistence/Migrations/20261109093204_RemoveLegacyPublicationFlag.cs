using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyPublicationFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_approval",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_legacy",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "is_legacy_published",
                schema: "listings",
                table: "listings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_legacy_published",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Restoring the old schema must preserve public listings without review history.
            migrationBuilder.Sql("""
                UPDATE listings.listings SET is_legacy_published = true
                WHERE status = 'PUBLISHED' AND (reviewed_by IS NULL OR reviewed_by <= 0 OR reviewed_at IS NULL);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_approval",
                schema: "listings",
                table: "listings",
                sql: "status <> 'PUBLISHED' OR is_legacy_published OR (reviewed_by IS NOT NULL AND reviewed_by > 0 AND reviewed_at IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_legacy",
                schema: "listings",
                table: "listings",
                sql: "NOT is_legacy_published OR status = 'PUBLISHED'");
        }
    }
}
