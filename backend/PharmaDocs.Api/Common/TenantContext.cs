using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.Common.Exceptions;
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
            var httpContext = _http.HttpContext;

            // Geen HttpContext = achtergrond-/opstartwerk (bv. de DbSeeder of het
            // toepassen van migraties): val terug op de default-organisatie.
            if (httpContext is null)
                return Organization.DefaultId;

            var value = httpContext.User.FindFirst("tenant")?.Value;
            if (Guid.TryParse(value, out var id))
                return id;

            // Een geauthenticeerd verzoek zonder geldige tenant-claim is een anomalie
            // (bv. een oud token van vóór de multi-tenant claim): weiger het i.p.v.
            // stilzwijgend op een tenant terug te vallen (zou cross-tenant kunnen lekken).
            if (httpContext.User.Identity?.IsAuthenticated == true)
                throw new UnauthorizedException("Geen geldige tenant in het token. Log opnieuw in.");

            // Niet-geauthenticeerd (bv. de login zelf): nog geen tenant nodig; de User-
            // tabel heeft geen tenant-filter, dus deze waarde wordt niet gebruikt.
            return Organization.DefaultId;
        }
    }
}
