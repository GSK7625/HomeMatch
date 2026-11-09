using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Viewings.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialViewingsSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "viewings");

        migrationBuilder.CreateTable(
            name: "viewing_appointments",
            schema: "viewings",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                tenant_id = table.Column<long>(type: "bigint", nullable: false),
                listing_id = table.Column<long>(type: "bigint", nullable: false),
                appointment_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                duration_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                cancelled_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                cancelled_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                remind_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_reminded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_viewing_appointments", x => x.id);
                table.CheckConstraint("ck_appointments_cancelled_by", "cancelled_by IS NULL OR cancelled_by IN ('TENANT', 'LANDLORD', 'SYSTEM')");
                table.CheckConstraint("ck_appointments_duration", "duration_minutes BETWEEN 15 AND 180");
                table.CheckConstraint("ck_appointments_status", "status IN ('PENDING', 'CONFIRMED', 'CANCELLED', 'COMPLETED')");
            });

        migrationBuilder.CreateTable(
            name: "virtual_viewings",
            schema: "viewings",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                appointment_id = table.Column<long>(type: "bigint", nullable: false),
                room_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SCHEDULED"),
                started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_virtual_viewings", x => x.id);
                table.CheckConstraint("ck_virtualviewings_status", "status IN ('SCHEDULED', 'IN_PROGRESS', 'ENDED', 'CANCELLED')");
                table.ForeignKey(
                    name: "FK_virtual_viewings_viewing_appointments_appointment_id",
                    column: x => x.appointment_id,
                    principalSchema: "viewings",
                    principalTable: "viewing_appointments",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_viewing_appointments_listing_id",
            schema: "viewings",
            table: "viewing_appointments",
            column: "listing_id");

        migrationBuilder.CreateIndex(
            name: "ix_viewing_appointments_listing_time",
            schema: "viewings",
            table: "viewing_appointments",
            columns: new[] { "listing_id", "appointment_time" });

        migrationBuilder.CreateIndex(
            name: "ix_viewing_appointments_tenant_id",
            schema: "viewings",
            table: "viewing_appointments",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "uq_virtualviewings_app",
            schema: "viewings",
            table: "virtual_viewings",
            column: "appointment_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "virtual_viewings",
            schema: "viewings");

        migrationBuilder.DropTable(
            name: "viewing_appointments",
            schema: "viewings");
    }
}
