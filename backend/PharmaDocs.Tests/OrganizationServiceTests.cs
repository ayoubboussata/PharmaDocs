using Moq;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.DTOs.Organizations;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Tests;

/// <summary>
/// Tests voor de onboarding: een operator maakt een organisatie aan met haar eerste
/// tenant-admin (Fase 3).
/// </summary>
public class OrganizationServiceTests
{
    private static (OrganizationService svc, Mock<IOrganizationRepository> orgs, Mock<IUserRepository> users) Build()
    {
        var orgs = new Mock<IOrganizationRepository>();
        orgs.Setup(o => o.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        orgs.Setup(o => o.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return (new OrganizationService(orgs.Object, users.Object), orgs, users);
    }

    private static CreateOrganizationRequest Req(
        string name = "Apotheek De Nieuwe", string? slug = null,
        string email = "admin@nieuw.be", string password = "wachtwoord123") =>
        new(name, slug, email, password);

    [Fact]
    public async Task Provision_maakt_org_en_tenant_admin()
    {
        var (svc, orgs, users) = Build();
        Organization? createdOrg = null;
        User? createdAdmin = null;
        orgs.Setup(o => o.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((o, _) => createdOrg = o).Returns(Task.CompletedTask);
        users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdAdmin = u).Returns(Task.CompletedTask);

        var result = await svc.ProvisionAsync(Req(name: "Apotheek De Nieuwe", email: "  Admin@Nieuw.BE "));

        Assert.NotNull(createdOrg);
        Assert.Equal("apotheek-de-nieuwe", createdOrg!.Slug); // slug afgeleid uit de naam
        Assert.Equal(result.Id, createdOrg.Id);

        Assert.NotNull(createdAdmin);
        Assert.Equal(createdOrg.Id, createdAdmin!.TenantId);   // admin in de nieuwe tenant
        Assert.Equal(UserRole.Admin, createdAdmin.Role);
        Assert.Equal("admin@nieuw.be", createdAdmin.Email);    // genormaliseerd
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("wachtwoord123", createdAdmin.PasswordHash));
    }

    [Fact]
    public async Task Provision_bestaande_email_geeft_Conflict_zonder_org_aan_te_maken()
    {
        var (svc, orgs, users) = Build();
        users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => svc.ProvisionAsync(Req()));

        // Geen weesorganisatie: de org wordt niet aangemaakt als de admin al bestaat.
        orgs.Verify(o => o.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Provision_bestaande_slug_geeft_Conflict()
    {
        var (svc, orgs, users) = Build();
        orgs.Setup(o => o.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => svc.ProvisionAsync(Req()));
        users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
