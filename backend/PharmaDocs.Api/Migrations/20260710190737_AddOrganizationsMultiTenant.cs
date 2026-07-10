using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaDocs.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsMultiTenant : Migration
    {
        /// <inheritdoc />
        // Vaste id van de default-organisatie (== Organization.DefaultId). Alle
        // bestaande single-tenant data wordt hieraan gekoppeld (backfill).
        private static readonly Guid DefaultOrgId = new("11111111-1111-1111-1111-111111111111");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // De oude enkelvoudige bron-index wijkt voor de tenant-scoped variant.
            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_SourceName",
                table: "KnowledgeChunks");

            // 1. De Organizations-tabel + de default-organisatie aanmaken.
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Name", "Slug", "CreatedAt" },
                values: new object[]
                {
                    DefaultOrgId,
                    "Apotheek De Wit",
                    "apotheek-de-wit",
                    new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
                });

            // 2. TenantId eerst nullable toevoegen, dan de bestaande rijen backfillen
            //    naar de default-organisatie, dan pas non-nullable maken. Zo blijft de
            //    bestaande data behouden en voldoet ze meteen aan de FK.
            foreach (var table in new[] { "Users", "Documents", "KnowledgeChunks" })
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "TenantId",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.Sql(
                    $"UPDATE \"{table}\" SET \"TenantId\" = '{DefaultOrgId}' WHERE \"TenantId\" IS NULL;");

                migrationBuilder.AlterColumn<Guid>(
                    name: "TenantId",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    oldClrType: typeof(Guid),
                    oldType: "uuid",
                    oldNullable: true);
            }

            // 3. Indexen + foreign keys (alle rijen wijzen nu naar een bestaande org).
            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId",
                table: "Documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_TenantId_SourceName",
                table: "KnowledgeChunks",
                columns: new[] { "TenantId", "SourceName" });

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Organizations_TenantId",
                table: "Documents",
                column: "TenantId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeChunks_Organizations_TenantId",
                table: "KnowledgeChunks",
                column: "TenantId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Organizations_TenantId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeChunks_Organizations_TenantId",
                table: "KnowledgeChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_TenantId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_TenantId_SourceName",
                table: "KnowledgeChunks");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KnowledgeChunks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_SourceName",
                table: "KnowledgeChunks",
                column: "SourceName");
        }
    }
}
