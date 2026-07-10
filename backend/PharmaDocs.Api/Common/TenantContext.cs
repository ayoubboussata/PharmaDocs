using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Common;

/// <summary>
/// Leest de huidige tenant uit de <c>tenant</c>-claim van het ingelogde JWT.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _http;

    public TenantContext(IHttpContextAccessor http) => _http = http;

    public Guid TenantId
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirst("tenant")?.Value;
            if (Guid.TryParse(value, out var id))
                return id;

            // Terugval op de default-organisatie:
            //  - bij achtergrond-/opstartwerk (geen HttpContext, bv. de seeder);
            //  - bij een oud token van vóór de tenant-claim (eenmalig opnieuw inloggen).
            // Veilig zolang er één organisatie is. Fase 3 (echte multi-tenant onboarding)
            // maakt een ontbrekende claim op een geauthenticeerd verzoek een harde fout.
            return Organization.DefaultId;
        }
    }
}
