using Moq;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Tests;

/// <summary>
/// Tests voor de RAG-kennisassistent: de ask-flow (embed → zoek → antwoord) en de
/// ingest-flow (validatie + transactionele herindexering).
/// </summary>
public class KnowledgeServiceTests
{
    private static (KnowledgeService svc, Mock<IKnowledgeRepository> repo,
        Mock<IEmbeddingClient> embed, Mock<IRagAnswerClient> answer) Build()
    {
        var repo = new Mock<IKnowledgeRepository>();
        var embed = new Mock<IEmbeddingClient>();
        var answer = new Mock<IRagAnswerClient>();
        var svc = new KnowledgeService(repo.Object, embed.Object, answer.Object);
        return (svc, repo, embed, answer);
    }

    // --- AskAsync ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Lege_vraag_geeft_BadRequest(string question)
    {
        var (svc, _, _, _) = Build();
        await Assert.ThrowsAsync<BadRequestException>(() => svc.AskAsync(question));
    }

    [Fact]
    public async Task Ask_embedt_de_vraag_zoekt_en_antwoordt_met_unieke_bronnen()
    {
        var (svc, repo, embed, answer) = Build();
        const string vraag = "Wat zijn de openingsuren?";
        var vector = new[] { 0.1f, 0.2f, 0.3f };

        embed.Setup(e => e.EmbedQueryAsync(vraag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vector);
        // Twee treffers uit dezelfde bron → moet één keer in Sources verschijnen.
        repo.Setup(r => r.SearchAsync(vector, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk>
            {
                new("openingsuren.pdf", "Wij openen om 9u.", 0.10),
                new("openingsuren.pdf", "Op zondag gesloten.", 0.22),
            });
        answer.Setup(a => a.AnswerAsync(vraag, It.IsAny<IReadOnlyList<RagContext>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Om 9u; zondag gesloten (bron: openingsuren.pdf).");

        var result = await svc.AskAsync(vraag);

        Assert.Contains("9u", result.Answer);
        Assert.Single(result.Sources);
        Assert.Equal("openingsuren.pdf", result.Sources[0]);
    }

    [Fact]
    public async Task Ask_geeft_de_opgehaalde_fragmenten_als_context_door_aan_Claude()
    {
        var (svc, repo, embed, answer) = Build();
        var vector = new[] { 0.5f };
        embed.Setup(e => e.EmbedQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vector);
        repo.Setup(r => r.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk> { new("koelketen.pdf", "Bewaar tussen 2 en 8 °C.", 0.05) });
        answer.Setup(a => a.AnswerAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<RagContext>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("2–8 °C (bron: koelketen.pdf).");

        await svc.AskAsync("Hoe bewaar ik koelkastmedicatie?");

        // De context die naar de AI-service gaat, moet de opgehaalde bron + inhoud bevatten.
        answer.Verify(a => a.AnswerAsync(
            It.IsAny<string>(),
            It.Is<IReadOnlyList<RagContext>>(c =>
                c.Count == 1 && c[0].SourceName == "koelketen.pdf" && c[0].Content.Contains("2 en 8")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- IngestAsync ---

    [Fact]
    public async Task Ingest_niet_pdf_geeft_UnsupportedMediaType()
    {
        var (svc, repo, embed, _) = Build();
        await Assert.ThrowsAsync<UnsupportedMediaTypeException>(
            () => svc.IngestAsync(TestData.FakeFile("procedure.txt", "text/plain")));

        // Bij een validatiefout mag er niets geëmbed of opgeslagen worden.
        embed.Verify(e => e.EmbedDocumentAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.ReplaceSourceAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyList<KnowledgeChunk>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ingest_zonder_pdf_magic_bytes_wordt_geweigerd()
    {
        var (svc, _, _, _) = Build();
        // .pdf-naam maar geen %PDF-header → magic-byte-check (nu ook op de kennisupload).
        var nep = TestData.FakeFile("procedure.pdf", "application/pdf",
            content: new byte[] { 0x00, 0x01, 0x02, 0x03 });
        await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => svc.IngestAsync(nep));
    }

    [Fact]
    public async Task Ingest_embedt_en_vervangt_de_bron_transactioneel()
    {
        var (svc, repo, embed, _) = Build();
        embed.Setup(e => e.EmbedDocumentAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmbeddedChunk>
            {
                new(0, "Fragment één.", new[] { 0.1f, 0.2f }),
                new(1, "Fragment twee.", new[] { 0.3f, 0.4f }),
            });

        var result = await svc.IngestAsync(TestData.FakeFile("procedure.pdf"));

        Assert.Equal("procedure.pdf", result.SourceName);
        Assert.Equal(2, result.ChunkCount);
        repo.Verify(r => r.ReplaceSourceAsync(
            "procedure.pdf",
            It.Is<IReadOnlyList<KnowledgeChunk>>(c => c.Count == 2 && c[0].SourceName == "procedure.pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ingest_neemt_enkel_de_bestandsnaam_zonder_pad()
    {
        var (svc, repo, embed, _) = Build();
        embed.Setup(e => e.EmbedDocumentAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmbeddedChunk> { new(0, "Inhoud.", new[] { 0.1f }) });

        // Een client die een pad-prefix meestuurt, mag geen aparte bron worden.
        var result = await svc.IngestAsync(TestData.FakeFile("map/sub/procedure.pdf"));

        Assert.Equal("procedure.pdf", result.SourceName);
        repo.Verify(r => r.ReplaceSourceAsync(
            "procedure.pdf", It.IsAny<IReadOnlyList<KnowledgeChunk>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
