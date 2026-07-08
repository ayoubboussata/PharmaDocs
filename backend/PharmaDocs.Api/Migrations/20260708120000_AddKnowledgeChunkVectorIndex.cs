using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaDocs.Api.Migrations
{
    /// <summary>
    /// Voegt een HNSW-index (cosinus) toe op de embedding-kolom. Zonder deze index
    /// doet elke RAG-zoekopdracht een sequentiële scan + volledige afstandsberekening
    /// over álle chunks; met de index blijft de vectorzoektocht schaalbaar.
    /// Ruwe SQL omdat EF Core het pgvector-indextype niet native kan modelleren.
    /// </summary>
    /// <inheritdoc />
    public partial class AddKnowledgeChunkVectorIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_KnowledgeChunks_Embedding\" " +
                "ON \"KnowledgeChunks\" USING hnsw (\"Embedding\" vector_cosine_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_KnowledgeChunks_Embedding\";");
        }
    }
}
