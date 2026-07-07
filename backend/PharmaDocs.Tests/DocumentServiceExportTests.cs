using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Tests;

/// <summary>Tests voor de CSV-export (opmaak, escaping, selectie).</summary>
public class DocumentServiceExportTests
{
    private static DocumentService BuildService(IReadOnlyList<Document> docs)
    {
        var repo = new Mock<IDocumentRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);
        return new DocumentService(
            repo.Object, Mock.Of<IInvoiceExtractionClient>(), NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task Export_begint_met_utf8_bom_en_bevat_header()
    {
        var service = BuildService(new[] { TestData.Doc("Acme", "F-1", 100m, 21m, 121m) });

        var bytes = await service.ExportCsvAsync(Guid.NewGuid(), null);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Bestand;Leverancier;Factuurnummer", csv);
    }

    [Fact]
    public async Task Export_bevat_categorie_en_belgische_getalnotatie()
    {
        var service = BuildService(new[] { TestData.Doc("Acme", "F-1", 100m, 21m, 121m) });

        var csv = Encoding.UTF8.GetString(await service.ExportCsvAsync(Guid.NewGuid(), null));

        Assert.Contains("Geneesmiddelen", csv);
        Assert.Contains("121,00", csv); // komma als decimaalteken (nl-BE)
    }

    [Fact]
    public async Task Export_escapet_puntkomma_in_een_veld()
    {
        var service = BuildService(new[] { TestData.Doc("Acme; NV", "F-2", 10m, 2m, 12m) });

        var csv = Encoding.UTF8.GetString(await service.ExportCsvAsync(Guid.NewGuid(), null));

        Assert.Contains("\"Acme; NV\"", csv); // veld met ; tussen quotes
    }

    [Fact]
    public async Task Export_met_ids_neemt_enkel_de_selectie_mee()
    {
        var acme = TestData.Doc("Acme", "F-1", 100m, 21m, 121m);
        var beta = TestData.Doc("Beta", "F-2", 50m, 10m, 60m);
        var service = BuildService(new[] { acme, beta });

        var csv = Encoding.UTF8.GetString(await service.ExportCsvAsync(Guid.NewGuid(), new[] { acme.Id }));

        Assert.Contains("Acme", csv);
        Assert.DoesNotContain("Beta", csv);
    }
}
