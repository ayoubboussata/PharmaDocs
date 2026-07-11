using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaDocs.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAccentColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Organizations",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "#2563eb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Organizations");
        }
    }
}
