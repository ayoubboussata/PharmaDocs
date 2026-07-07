using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Tests;

/// <summary>Tests voor de upload-flow: validatie en de "enkel facturen"-poort.</summary>
public class DocumentServiceUploadTests
{
    private static (DocumentService svc, Mock<IDocumentRepository> repo, Mock<IInvoiceExtractionClient> client) Build()
    {
        var repo = new Mock<IDocumentRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeleteAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var client = new Mock<IInvoiceExtractionClient>();
        var svc = new DocumentService(repo.Object, client.Object, NullLogger<DocumentService>.Instance);
        return (svc, repo, client);
    }

    private static InvoiceExtractionResult Result(bool isInvoice, string supplier = "Acme") =>
        new(isInvoice, supplier, "F-1", null, 100m, 21m, 21m, 121m, "EUR", "Geneesmiddelen",
            new List<InvoiceLineResult>());

    [Fact]
    public async Task Leeg_bestand_geeft_BadRequest()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAsync<BadRequestException>(
            () => svc.UploadAndExtractAsync(TestData.FakeFile(size: 0), Guid.NewGuid()));
    }

    [Fact]
    public async Task Te_groot_bestand_geeft_PayloadTooLarge()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAsync<PayloadTooLargeException>(
            () => svc.UploadAndExtractAsync(TestData.FakeFile(size: 11 * 1024 * 1024), Guid.NewGuid()));
    }

    [Fact]
    public async Task Niet_pdf_geeft_UnsupportedMediaType()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAsync<UnsupportedMediaTypeException>(
            () => svc.UploadAndExtractAsync(TestData.FakeFile("brief.txt", "text/plain"), Guid.NewGuid()));
    }

    [Fact]
    public async Task Geen_factuur_wordt_geweigerd_en_het_document_verwijderd()
    {
        var (svc, repo, client) = Build();
        client.Setup(c => c.ExtractAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result(isInvoice: false));

        await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => svc.UploadAndExtractAsync(TestData.FakeFile(), Guid.NewGuid()));

        repo.Verify(r => r.DeleteAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Geldige_factuur_wordt_verwerkt()
    {
        var (svc, _, client) = Build();
        client.Setup(c => c.ExtractAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result(isInvoice: true, supplier: "Acme"));

        var dto = await svc.UploadAndExtractAsync(TestData.FakeFile(), Guid.NewGuid());

        Assert.Equal("Processed", dto.Status);
        Assert.Equal("Acme", dto.ExtractedInvoice!.SupplierName);
    }
}
