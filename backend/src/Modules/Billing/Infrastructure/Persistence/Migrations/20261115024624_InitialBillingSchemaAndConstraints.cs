using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBillingSchemaAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    landlord_id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    property_id = table.Column<long>(type: "bigint", nullable: false),
                    billing_month = table.Column<int>(type: "integer", nullable: false),
                    billing_year = table.Column<int>(type: "integer", nullable: false),
                    rent_amount = table.Column<long>(type: "bigint", nullable: false),
                    electricity_amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    water_amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    service_amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    discount_amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    total_amount = table.Column<long>(type: "bigint", nullable: false),
                    paid_amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "UNPAID"),
                    cancel_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.CheckConstraint("ck_invoices_month", "billing_month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_invoices_paid_limit", "paid_amount <= total_amount");
                    table.CheckConstraint("ck_invoices_rent", "rent_amount >= 0");
                    table.CheckConstraint("ck_invoices_status", "status IN ('UNPAID', 'OVERDUE', 'PAID', 'CANCELLED')");
                    table.CheckConstraint("ck_invoices_year", "billing_year >= 2020");
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "VNPAY"),
                    gateway_transaction_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    card_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    gateway_response_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    payment_url = table.Column<string>(type: "text", nullable: true),
                    proof_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    raw_ipn_data = table.Column<string>(type: "text", nullable: true),
                    verified_by = table.Column<long>(type: "bigint", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_payments_method", "payment_method IN ('VNPAY', 'BANK_TRANSFER', 'CASH', 'MOMO')");
                    table.CheckConstraint("ck_payments_status", "status IN ('PENDING', 'SUCCESS', 'FAILED', 'EXPIRED')");
                    table.ForeignKey(
                        name: "FK_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_landlord_property",
                schema: "billing",
                table: "invoices",
                columns: new[] { "landlord_id", "property_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_status",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_invoices_code",
                schema: "billing",
                table: "invoices",
                column: "invoice_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_invoices_contract_period_active",
                schema: "billing",
                table: "invoices",
                columns: new[] { "contract_id", "billing_month", "billing_year" },
                unique: true,
                filter: "status <> 'CANCELLED'");

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_status",
                schema: "billing",
                table: "payments",
                columns: new[] { "invoice_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_payments_code",
                schema: "billing",
                table: "payments",
                column: "payment_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payments_invoice_single_pending",
                schema: "billing",
                table: "payments",
                column: "invoice_id",
                unique: true,
                filter: "status = 'PENDING'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "billing");
        }
    }
}
