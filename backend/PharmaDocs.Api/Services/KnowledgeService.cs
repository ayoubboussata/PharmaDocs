using Microsoft.AspNetCore.Http;
using Pgvector;
using PharmaDocs.Api.Common;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

public class KnowledgeService : IKnowledgeService
{
    private const int TopK = 4; // aantal fragmenten dat als context naar Claude gaat

    private readonly IKnowledgeRepository _repository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IRagAnswerClient _answerClient;
    private readonly ITenantContext _tenant;

    public KnowledgeService(
        IKnowledgeRepository repository,
        IEmbeddingClient embeddingClient,
        IRagAnswerClient answerClient,
        ITenantContext tenant)
    {
        _repository = repository;
        _embeddingClient = embeddingClient;
        _answerClient = answerClient;
        _tenant = tenant;
    }

    public async Task<KnowledgeIngestResponse> IngestAsync(IFormFile file, CancellationToken ct = default)
    {
        PdfUploadValidator.Validate(file);

        // De kennisbank is bewust gedeeld en op bestandsnaam gesleuteld (re-upload =
        // herindexeren). Neem enkel het bestandsnaam-deel: zo kan een niet-browserclient
        // niet met een pad-prefix ("map/x.pdf" vs "x.pdf") stiekem een tweede kopie
        // maken van dezelfde bron.
        var sourceName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new BadRequestException("Ongeldige bestandsnaam.");

        IReadOnlyList<EmbeddedChunk> embedded;
        await using (var stream = file.OpenReadStream())
        {
            embedded = await _embeddingClient.EmbedDocumentAsync(stream, sourceName, file.ContentType, ct);
        }

        var now = DateTime.UtcNow;
        var chunks = embedded.Select(c => new KnowledgeChunk
        {
            TenantId = _tenant.TenantId,   // kennisstukken zijn tenant-gescoped (geen cross-tenant lek)
            SourceName = sourceName,
            ChunkIndex = c.Index,
            Content = c.Content,
            Embedding = new Vector(c.Embedding),
            CreatedAt = now,
        }).ToList();

        // Herindexeren in één transactie: de oude stukken van deze bron eruit en de
        // nieuwe erin. Zo kan een herindexering nooit half slagen en de bron leeg
        // achterlaten (voordien: delete + add zonder transactie).
        await _repository.ReplaceSourceAsync(sourceName, chunks, ct);
        return new KnowledgeIngestResponse(sourceName, chunks.Count);
    }

    public Task<IReadOnlyList<KnowledgeSourceDto>> GetSourcesAsync(CancellationToken ct = default)
        => _repository.GetSourcesAsync(ct);

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new BadRequestException("Stel een vraag.");

        // 1. Vraag embedden → 2. dichtste stukken ophalen (pgvector).
        var queryVector = await _embeddingClient.EmbedQueryAsync(question, ct);
        var chunks = await _repository.SearchAsync(queryVector, TopK, ct);

        // 3. Fragmenten als context naar Claude, dat gegrond antwoordt met bronvermelding.
        var contexts = chunks.Select(c => new RagContext(c.SourceName, c.Content)).ToList();
        var answer = await _answerClient.AnswerAsync(question, contexts, ct);

        var sources = chunks.Select(c => c.SourceName).Distinct().ToList();
        return new AskResponse(answer, sources);
    }
}
