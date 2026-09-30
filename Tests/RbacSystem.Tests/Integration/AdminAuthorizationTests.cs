using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Common;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;

namespace RbacSystem.Tests.Integration;

public class AdminAuthorizationTests(WebApplicationFactoryFixture fixture)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private static string IssueToken(UserRole role)
    {
        JwtOptions jwt = new()
        {
            Issuer = AuthApiFactory.Issuer,
            Audience = AuthApiFactory.Audience,
            Key = AuthApiFactory.SigningKey,
            RefreshTokenHashSecret = AuthApiFactory.RefreshHashSecret
        };

        JwtTokenService tokenService = new(
            Options.Create(jwt),
            Options.Create(new AuthTokenOptions()),
            new RefreshTokenHasher(Options.Create(jwt)),
            new FakeTimeProvider(DateTimeOffset.UtcNow));

        User user = new()
        {
            Email = "admin-test@example.com",
            Name = "admin-tester",
            PasswordHash = "$2a$12$hash",
            Role = role
        };

        PreparedTokenPair prepared = tokenService.IssueTokenPair(
            user,
            "11111111-1111-1111-1111-111111111111",
            null,
            null);

        return prepared.Tokens.AccessToken;
    }

    private static string CreateTokenWithoutRole()
    {
        SigningCredentials credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthApiFactory.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        DateTime issuedAt = DateTime.UtcNow;
        DateTime expiresAt = issuedAt.AddMinutes(15);

        Dictionary<string, object> claims = new(StringComparer.Ordinal)
        {
            [JwtRegisteredClaimNames.Sub] = EntityId.New(),
            [JwtRegisteredClaimNames.Email] = "norole@example.com",
            [JwtRegisteredClaimNames.Sid] = EntityId.New(),
            [JwtRegisteredClaimNames.Jti] = EntityId.New(),
            [JwtTokenService.TokenVersionClaim] = "0"
        };

        SecurityTokenDescriptor descriptor = new()
        {
            Claims = claims,
            Issuer = AuthApiFactory.Issuer,
            Audience = AuthApiFactory.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private HttpClient CreateClient(string? accessToken = null)
    {
        HttpClient client = fixture.Factory.CreateClient();

        if (accessToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    [Fact]
    public async Task GetAdminEndpoint_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        HttpResponseMessage response = await CreateClient().GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminEndpoint_WithInvalidToken_ShouldReturn401Unauthorized()
    {
        // Act
        HttpResponseMessage response = await CreateClient("invalid.jwt.token").GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminEndpoint_WithNonAdminRole_ShouldReturn403Forbidden()
    {
        // Arrange
        string token = IssueToken(UserRole.User);

        // Act
        HttpResponseMessage response = await CreateClient(token).GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminEndpoint_WithoutAnyRole_ShouldReturn403Forbidden()
    {
        // Arrange
        string token = CreateTokenWithoutRole();

        // Act
        HttpResponseMessage response = await CreateClient(token).GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminEndpoint_WithAdminRole_ShouldReturn200Ok()
    {
        // Arrange
        string token = IssueToken(UserRole.Admin);

        // Act
        HttpResponseMessage response = await CreateClient(token).GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
