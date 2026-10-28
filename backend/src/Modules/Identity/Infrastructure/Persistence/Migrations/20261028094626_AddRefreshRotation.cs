using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedByCredentialId",
                schema: "identity",
                table: "RefreshCredentials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RefreshCredentials_Id_SessionId",
                schema: "identity",
                table: "RefreshCredentials",
                columns: new[] { "Id", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshCredentials_ReplacedByCredentialId_SessionId",
                schema: "identity",
                table: "RefreshCredentials",
                columns: new[] { "ReplacedByCredentialId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "RefreshCredentials",
                column: "SessionId",
                unique: true,
                filter: "\"UsedAt\" IS NULL AND \"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials",
                column: "ReplacedByCredentialId",
                unique: true,
                filter: "\"ReplacedByCredentialId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials",
                sql: "\"ReplacedByCredentialId\" IS NULL OR (\"UsedAt\" IS NOT NULL AND \"ReplacedByCredentialId\" <> \"Id\")");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshCredentials_RefreshCredentials_ReplacedByCredentialI~",
                schema: "identity",
                table: "RefreshCredentials",
                columns: new[] { "ReplacedByCredentialId", "SessionId" },
                principalSchema: "identity",
                principalTable: "RefreshCredentials",
                principalColumns: new[] { "Id", "SessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshCredentials_RefreshCredentials_ReplacedByCredentialI~",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RefreshCredentials_Id_SessionId",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropIndex(
                name: "IX_RefreshCredentials_ReplacedByCredentialId_SessionId",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropColumn(
                name: "ReplacedByCredentialId",
                schema: "identity",
                table: "RefreshCredentials");
        }
    }
}
