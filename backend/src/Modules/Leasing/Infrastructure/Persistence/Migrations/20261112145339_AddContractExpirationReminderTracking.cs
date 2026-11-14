using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Leasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractExpirationReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_expiration_reminder_sent_at",
                schema: "leasing",
                table: "contracts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_expiration_reminder_sent_at",
                schema: "leasing",
                table: "contracts");
        }
    }
}
