using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireAdminApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_status",
                schema: "listings",
                table: "listings");

            migrationBuilder.AddColumn<bool>(
                name: "is_legacy_published",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                schema: "listings",
                table: "listings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                schema: "listings",
                table: "listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "reviewed_by",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "revision",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateIndex(
                name: "ix_listings_pending_review",
                schema: "listings",
                table: "listings",
                columns: new[] { "updated_at", "id" },
                filter: "status = 'PENDING_APPROVAL'");

            // Preserve existing public listings without inventing an Admin review.
            migrationBuilder.Sql("UPDATE listings.listings SET is_legacy_published = true WHERE status = 'PUBLISHED';");

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_rejection",
                schema: "listings",
                table: "listings",
                sql: "status <> 'REJECTED' OR (rejection_reason IS NOT NULL AND length(btrim(rejection_reason)) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_revision",
                schema: "listings",
                table: "listings",
                sql: "revision > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_status",
                schema: "listings",
                table: "listings",
                sql: "status IN ('DRAFT', 'PENDING_APPROVAL', 'REJECTED', 'PUBLISHED', 'HIDDEN', 'EXPIRED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A rollback must never turn an unreviewed listing into a public listing.
            migrationBuilder.Sql("UPDATE listings.listings SET status = 'DRAFT' WHERE status IN ('PENDING_APPROVAL', 'REJECTED');");

            migrationBuilder.DropIndex(
                name: "ix_listings_pending_review",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_approval",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_legacy",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_rejection",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_revision",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_status",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "is_legacy_published",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "revision",
                schema: "listings",
                table: "listings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_status",
                schema: "listings",
                table: "listings",
                sql: "status IN ('DRAFT', 'PUBLISHED', 'HIDDEN', 'EXPIRED')");
        }
    }
}
