using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantLandlordDashboardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS billing.contracts;
                DROP TABLE IF EXISTS billing.dashboard_invoices;
                DROP TABLE IF EXISTS billing.rooms;
                DROP TABLE IF EXISTS billing.viewing_appointments;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContractCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DepositStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_invoices",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingMonth = table.Column<int>(type: "integer", nullable: false),
                    BillingYear = table.Column<int>(type: "integer", nullable: false),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ElectricityAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RemainingDebt = table.Column<decimal>(type: "numeric", nullable: false),
                    RentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RoomId = table.Column<long>(type: "bigint", nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TenantPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    WaterAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BuildingName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "viewing_appointments",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduledTime = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TenantPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ViewingType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viewing_appointments", x => x.Id);
                });
        }
    }
}
