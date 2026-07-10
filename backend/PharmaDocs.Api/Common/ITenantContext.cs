namespace PharmaDocs.Api.Common;

/// <summary>
/// De tenant (apotheek) waartoe het huidige verzoek behoort. Scoped per request;
/// gevoed uit de <c>tenant</c>-claim in het JWT. Alle tenant-data wordt hierop
/// gefilterd (EF global query filter) en nieuwe rijen krijgen deze tenant.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
