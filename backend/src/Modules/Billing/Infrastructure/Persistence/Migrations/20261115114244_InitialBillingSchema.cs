using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBillingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DepositStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomId = table.Column<long>(type: "bigint", nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TenantPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BillingMonth = table.Column<int>(type: "integer", nullable: false),
                    BillingYear = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ElectricityAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    WaterAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ServiceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingDebt = table.Column<decimal>(type: "numeric", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BuildingName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false)
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
                    ScheduledTime = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TenantPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ViewingType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viewing_appointments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contracts",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "rooms",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "viewing_appointments",
                schema: "billing");
        }
    }
}
