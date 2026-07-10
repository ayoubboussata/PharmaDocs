using Moq;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.DTOs.Auth;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Tests;

/// <summary>
/// Tests voor de auth-flow: registratie (admin maakt account) en login. Bewaakt de
/// belangrijkste beveiligingsgaranties — e-mailnormalisatie, gehasht wachtwoord en
/// één generieke fout voor "onbekende e-mail" én "fout wachtwoord".
/// </summary>
public class AuthServiceTests
{
    private static (AuthService svc, Mock<IUserRepository> users, Mock<ITokenService> tokens) Build()
    {
        var users = new Mock<IUserRepository>();
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.CreateToken(It.IsAny<User>()))
            .Returns(new TokenResult("jwt-token", DateTime.UtcNow.AddMinutes(60)));
        return (new AuthService(users.Object, tokens.Object, TestData.Tenant()), users, tokens);
    }

    // Lage work factor: enkel om de verify-tak te testen, houdt de test snel.
    private static User UserWith(string email, string password, UserRole role = UserRole.User) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 6),
        Role = role,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Register_bestaande_email_geeft_Conflict()
    {
        var (svc, users, _) = Build();
        users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => svc.RegisterAsync(new RegisterRequest("nieuw@apotheek.be", "wachtwoord123")));
    }

    [Fact]
    public async Task Register_normaliseert_email_en_hasht_wachtwoord()
    {
        var (svc, users, _) = Build();
        users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        User? added = null;
        users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);

        var result = await svc.RegisterAsync(new RegisterRequest("  Nieuw@Apotheek.BE ", "wachtwoord123"));

        Assert.Equal("nieuw@apotheek.be", result.Email);
        Assert.Equal("User", result.Role); // admin-aangemaakte accounts zijn gewone gebruikers
        Assert.NotNull(added);
        Assert.NotEqual("wachtwoord123", added!.PasswordHash); // gehasht, nooit in klare tekst
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("wachtwoord123", added.PasswordHash));
    }

    [Fact]
    public async Task Login_onbekende_email_geeft_Unauthorized()
    {
        var (svc, users, _) = Build();
        users.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.LoginAsync(new LoginRequest("weg@apotheek.be", "watdanook")));
    }

    [Fact]
    public async Task Login_fout_wachtwoord_geeft_Unauthorized()
    {
        var (svc, users, _) = Build();
        users.Setup(u => u.GetByEmailAsync("jan@apotheek.be", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserWith("jan@apotheek.be", "juist-wachtwoord"));

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.LoginAsync(new LoginRequest("jan@apotheek.be", "fout-wachtwoord")));
    }

    [Fact]
    public async Task Login_juiste_gegevens_geeft_token_en_rol()
    {
        var (svc, users, tokens) = Build();
        users.Setup(u => u.GetByEmailAsync("jan@apotheek.be", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserWith("jan@apotheek.be", "juist-wachtwoord", UserRole.Admin));

        var result = await svc.LoginAsync(new LoginRequest(" Jan@Apotheek.be ", "juist-wachtwoord"));

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("jan@apotheek.be", result.Email);
        Assert.Equal("Admin", result.Role);
        tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Once);
    }
}
