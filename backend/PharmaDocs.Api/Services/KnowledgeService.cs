using Microsoft.AspNetCore.Http;
using Pgvector;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

public class KnowledgeService : IKnowledgeService
{
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB
    private const int TopK = 4; // aantal fragmenten dat als context naar Claude gaat

    private readonly IKnowledgeRepository _repository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IRagAnswerClient _answerClient;

    public KnowledgeService(
        IKnowledgeRepository repository,
        IEmbeddingClient embeddingClient,
        IRagAnswerClient answerClient)
    {
        _repository = repository;
        _embeddingClient = embeddingClient;
        _answerClient = answerClient;
    }

    public async Task<KnowledgeIngestResponse> IngestAsync(IFormFile file, CancellationToken ct = default)
    {
        ValidatePdf(file);

        IReadOnlyList<EmbeddedChunk> embedded;
        await using (var stream = file.OpenReadStream())
        {
            embedded = await _embeddingClient.EmbedDocumentAsync(stream, file.FileName, file.ContentType, ct);
        }

        // Herindexeren: eerst de oude stukken van deze bron weg, dan de nieuwe erin.
        await _repository.DeleteBySourceAsync(file.FileName, ct);

        var now = DateTime.UtcNow;
        var chunks = embedded.Select(c => new KnowledgeChunk
        {
            SourceName = file.FileName,
            ChunkIndex = c.Index,
            Content = c.Content,
            Embedding = new Vector(c.Embedding),
            CreatedAt = now,
        }).ToList();

        await _repository.AddChunksAsync(chunks, ct);
        return new KnowledgeIngestResponse(file.FileName, chunks.Count);
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

    private static void ValidatePdf(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Geen bestand ontvangen.");
        if (file.Length > MaxBytes)
            throw new PayloadTooLargeException("Bestand te groot (max. 10 MB).");

        var isPdf = file.ContentType is "application/pdf" or "application/octet-stream"
            || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new UnsupportedMediaTypeException("Enkel PDF-bestanden worden ondersteund.");
    }
}
