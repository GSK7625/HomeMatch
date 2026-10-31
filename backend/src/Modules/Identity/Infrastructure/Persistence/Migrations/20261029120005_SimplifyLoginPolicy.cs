using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyLoginPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Invalidate the old long-lived sessions before removing their marker.
            migrationBuilder.Sql("""
                UPDATE identity."RefreshCredentials" AS credential
                SET "RevokedAt" = GREATEST(CURRENT_TIMESTAMP, credential."CreatedAt")
                FROM identity."Sessions" AS session
                WHERE credential."SessionId" = session."Id" AND session."RememberMe"
                  AND credential."RevokedAt" IS NULL;
                UPDATE identity."Sessions"
                SET "RevokedAt" = GREATEST(CURRENT_TIMESTAMP, "CreatedAt")
                WHERE "RememberMe" AND "RevokedAt" IS NULL;
                UPDATE identity."Users"
                SET "LockoutEnabled" = FALSE, "LockoutEnd" = NULL, "AccessFailedCount" = 0;
                """);

            migrationBuilder.DropColumn(
                name: "RememberMe",
                schema: "identity",
                table: "Sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RememberMe",
                schema: "identity",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
