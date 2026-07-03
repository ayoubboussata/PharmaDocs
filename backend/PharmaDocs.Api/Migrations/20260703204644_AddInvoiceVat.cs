using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaDocs.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceVat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "ExtractedInvoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "ExtractedInvoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "ExtractedInvoices",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "ExtractedInvoices");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "ExtractedInvoices");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "ExtractedInvoices");
        }
    }
}
