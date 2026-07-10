using System.Text;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.DTOs.Organizations;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizations;
    private readonly IUserRepository _users;

    public OrganizationService(IOrganizationRepository organizations, IUserRepository users)
    {
        _organizations = organizations;
        _users = users;
    }

    public async Task<OrganizationResponse> ProvisionAsync(
        CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var slug = Slugify(string.IsNullOrWhiteSpace(request.Slug) ? name : request.Slug!);
        if (slug.Length == 0)
            throw new BadRequestException("Ongeldige naam/slug voor de organisatie.");

        var adminEmail = request.AdminEmail.Trim().ToLowerInvariant();

        // Uniciteit vooraf controleren (e-mail is globaal uniek): zo maken we geen
        // organisatie aan als de admin-account toch niet aangemaakt kan worden.
        if (await _users.ExistsByEmailAsync(adminEmail, ct))
            throw new ConflictException("Er bestaat al een account met dit e-mailadres.");
        if (await _organizations.SlugExistsAsync(slug, ct))
            throw new ConflictException("Er bestaat al een organisatie met deze slug.");

        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            CreatedAt = now,
        };
        await _organizations.AddAsync(organization, ct);

        // Eerste tenant-admin van de nieuwe organisatie.
        await _users.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            TenantId = organization.Id,
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.AdminPassword, workFactor: 12),
            Role = UserRole.Admin,
            CreatedAt = now,
        }, ct);

        return ToResponse(organization);
    }

    public async Task<IReadOnlyList<OrganizationResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var organizations = await _organizations.GetAllAsync(ct);
        return organizations.Select(ToResponse).ToList();
    }

    private static OrganizationResponse ToResponse(Organization o) =>
        new(o.Id, o.Name, o.Slug, o.CreatedAt);

    /// <summary>
    /// Maakt een URL-vriendelijke slug: kleine letters, cijfers en woordscheidingen als
    /// koppelteken (bv. "Apotheek De Wit" → "apotheek-de-wit").
    /// </summary>
    private static string Slugify(string value)
    {
        var sb = new StringBuilder(value.Length);
        var lastWasDash = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && sb.Length > 0)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }
}
