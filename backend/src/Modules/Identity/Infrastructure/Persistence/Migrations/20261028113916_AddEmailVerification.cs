using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailVerificationChallenges",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EmailSnapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    NormalizedEmailSnapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    CodeMac = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PendingLeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvalidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationChallenges", x => x.Id);
                    table.CheckConstraint("CK_EmailVerification_Attempts", "\"FailedAttempts\" BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_EmailVerification_Consumed", "(\"State\" = 4) = (\"ConsumedAt\" IS NOT NULL) AND (\"ConsumedAt\" IS NULL OR (\"ConsumedAt\" >= \"CreatedAt\" AND \"ConsumedAt\" < \"ExpiresAt\"))");
                    table.CheckConstraint("CK_EmailVerification_Invalidated", "(\"State\" IN (3,5,6)) = (\"InvalidatedAt\" IS NOT NULL) AND (\"InvalidatedAt\" IS NULL OR \"InvalidatedAt\" >= \"CreatedAt\")");
                    table.CheckConstraint("CK_EmailVerification_KeyId", "\"KeyId\" ~ '^[A-Za-z0-9_-]{1,32}$'");
                    table.CheckConstraint("CK_EmailVerification_LastAttempt", "\"LastAttemptAt\" IS NULL OR \"LastAttemptAt\" >= \"CreatedAt\"");
                    table.CheckConstraint("CK_EmailVerification_Locked", "(\"State\" = 6) = (\"FailedAttempts\" = 5)");
                    table.CheckConstraint("CK_EmailVerification_Mac", "\"CodeMac\" ~ '^[0-9A-F]{64}$'");
                    table.CheckConstraint("CK_EmailVerification_Purpose", "\"Purpose\" = 'EmailVerification'");
                    table.CheckConstraint("CK_EmailVerification_State", "\"State\" BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_EmailVerification_Submitted", "(\"State\" NOT IN (2,4,6) OR \"SubmittedAt\" IS NOT NULL) AND (\"SubmittedAt\" IS NULL OR (\"SubmittedAt\" >= \"CreatedAt\" AND \"SubmittedAt\" < \"ExpiresAt\"))");
                    table.CheckConstraint("CK_EmailVerification_Time", "\"ExpiresAt\" = \"CreatedAt\" + interval '5 minutes' AND \"PendingLeaseUntil\" = \"CreatedAt\" + interval '2 minutes'");
                    table.ForeignKey(
                        name: "FK_EmailVerificationChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationQuotas",
                schema: "identity",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationQuotas", x => x.Key);
                    table.CheckConstraint("CK_EmailVerificationQuota_Count", "\"Count\" >= 0");
                    table.CheckConstraint("CK_EmailVerificationQuota_Window", "\"WindowEnd\" > \"WindowStart\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationChallenges_UserId_CreatedAt",
                schema: "identity",
                table: "EmailVerificationChallenges",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_EmailVerificationChallenges_Active",
                schema: "identity",
                table: "EmailVerificationChallenges",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "\"State\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationChallenges",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "EmailVerificationQuotas",
                schema: "identity");
        }
    }
}
