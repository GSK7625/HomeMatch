using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Leasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSignatureImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "landlord_signature_data",
                schema: "leasing",
                table: "contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tenant_signature_data",
                schema: "leasing",
                table: "contracts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "landlord_signature_data",
                schema: "leasing",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "tenant_signature_data",
                schema: "leasing",
                table: "contracts");
        }
    }
}
