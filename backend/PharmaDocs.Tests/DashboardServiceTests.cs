using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Tests;

/// <summary>Tests voor de dashboard-aggregatie.</summary>
public class DashboardServiceTests
{
    private static DashboardService Build(IReadOnlyList<Document> docs)
    {
        var repo = new Mock<IDocumentRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);
        return new DashboardService(repo.Object);
    }

    [Fact]
    public async Task Aggregeert_enkel_verwerkte_facturen()
    {
        var docs = new[]
        {
            TestData.Doc("Acme", "F-1", 100m, 21m, 121m, date: new DateOnly(2026, 6, 1)),
            TestData.Doc("Acme", "F-2", 200m, 42m, 242m, date: new DateOnly(2026, 6, 15)),
            TestData.Doc("Beta", "F-3", 50m, 10m, 60m, category: "Diensten", date: new DateOnly(2026, 5, 1)),
            TestData.Doc("Gamma", "F-4", 0m, 0m, 0m, status: DocumentStatus.Failed), // telt niet mee
        };

        var summary = await Build(docs).GetSummaryAsync(Guid.NewGuid());

        Assert.Equal(3, summary.InvoiceCount);            // Failed uitgesloten
        Assert.Equal(423m, summary.TotalSpend);           // 121 + 242 + 60
        Assert.Equal("Acme", summary.BySupplier[0].Label); // grootste eerst (363)
        Assert.Equal(363m, summary.BySupplier[0].Total);
    }

    [Fact]
    public async Task Groepeert_per_maand_chronologisch()
    {
        var docs = new[]
        {
            TestData.Doc("Acme", "F-1", 100m, 21m, 121m, date: new DateOnly(2026, 6, 1)),
            TestData.Doc("Beta", "F-2", 50m, 10m, 60m, date: new DateOnly(2026, 5, 1)),
        };

        var summary = await Build(docs).GetSummaryAsync(Guid.NewGuid());

        Assert.Equal(2, summary.ByMonth.Count);
        Assert.Equal("2026-05", summary.ByMonth[0].Month); // oplopend gesorteerd
        Assert.Equal("2026-06", summary.ByMonth[1].Month);
    }

    [Fact]
    public async Task Groepeert_per_categorie()
    {
        var docs = new[]
        {
            TestData.Doc("Acme", "F-1", 100m, 21m, 121m, category: "Geneesmiddelen"),
            TestData.Doc("Beta", "F-2", 50m, 10m, 60m, category: "Diensten"),
        };

        var summary = await Build(docs).GetSummaryAsync(Guid.NewGuid());

        Assert.Contains(summary.ByCategory, c => c.Label == "Geneesmiddelen");
        Assert.Contains(summary.ByCategory, c => c.Label == "Diensten");
    }
}
