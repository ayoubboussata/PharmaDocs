using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using PharmaDocs.Api.Common;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Tests;

/// <summary>
/// Tests voor de tenant-resolutie uit het JWT (Fase 2/3): terugval bij opstart, lezen
/// van de claim, en de harde weigering bij een geauthenticeerd verzoek zonder tenant.
/// </summary>
public class TenantContextTests
{
    private static ITenantContext Build(HttpContext? ctx)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);
        return new TenantContext(accessor.Object);
    }

    [Fact]
    public void Geen_httpcontext_valt_terug_op_de_default_org()
    {
        // Achtergrond-/opstartwerk (bv. de seeder) heeft geen HttpContext.
        Assert.Equal(Organization.DefaultId, Build(null).TenantId);
    }

    [Fact]
    public void Geldige_tenant_claim_wordt_gelezen()
    {
        var tenant = Guid.NewGuid();
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("tenant", tenant.ToString()) }, authenticationType: "test")),
        };

        Assert.Equal(tenant, Build(ctx).TenantId);
    }

    [Fact]
    public void Geauthenticeerd_zonder_tenant_claim_gooit_Unauthorized()
    {
        // Geldig ingelogd (authenticationType gezet → IsAuthenticated), maar geen tenant:
        // een oud token → weigeren i.p.v. stilzwijgend terugvallen.
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", Guid.NewGuid().ToString()) }, authenticationType: "test")),
        };

        Assert.Throws<UnauthorizedException>(() => Build(ctx).TenantId);
    }
}
