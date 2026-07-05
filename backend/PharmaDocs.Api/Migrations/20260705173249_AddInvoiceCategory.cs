using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaDocs.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ExtractedInvoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "ExtractedInvoices");
        }
    }
}
