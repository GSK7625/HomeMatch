using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshCredentials",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshCredentials", x => x.Id);
                    table.CheckConstraint("CK_RefreshCredentials_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_RefreshCredentials_Hash", "\"TokenHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_RefreshCredentials_Revoked", "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"CreatedAt\"");
                    table.CheckConstraint("CK_RefreshCredentials_Used", "\"UsedAt\" IS NULL OR \"UsedAt\" >= \"CreatedAt\"");
                    table.ForeignKey(
                        name: "FK_RefreshCredentials_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "identity",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshCredentials_SessionId",
                schema: "identity",
                table: "RefreshCredentials",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_TokenHash",
                schema: "identity",
                table: "RefreshCredentials",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshCredentials",
                schema: "identity");
        }
    }
}
