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

    private readonly IKnowledgeRepository _repository;
    private readonly IEmbeddingClient _embeddingClient;

    public KnowledgeService(IKnowledgeRepository repository, IEmbeddingClient embeddingClient)
    {
        _repository = repository;
        _embeddingClient = embeddingClient;
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
