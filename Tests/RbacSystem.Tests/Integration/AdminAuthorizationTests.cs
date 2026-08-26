using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Integration;

public class AdminAuthorizationTests(WebApplicationFactoryFixture fixture)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private static async Task<string> IssueTokenAsync(UserRole? role = null)
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
            new FakeRefreshTokenRepository(),
            new RefreshTokenHasher(Options.Create(jwt)),
            new FakeTimeProvider(DateTimeOffset.UtcNow));

        User user = new()
        {
            Email = "admin-test@example.com",
            Name = "admin-tester",
            PasswordHash = "$2a$12$hash",
            Role = role ?? UserRole.User
        };

        IssuedTokens tokens = await tokenService.IssueTokenPairAsync(
            user,
            "11111111-1111-1111-1111-111111111111",
            null,
            null);

        return tokens.AccessToken;
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
        string token = await IssueTokenAsync(UserRole.User);

        // Act
        HttpResponseMessage response = await CreateClient(token).GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminEndpoint_WithoutAnyRole_ShouldReturn403Forbidden()
    {
        // Arrange
        string token = await IssueTokenAsync(role: null);

        // Act
        HttpResponseMessage response = await CreateClient(token).GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminEndpoint_WithAdminRole_ShouldReturn200Ok()
    {
        // Arrange
        string token = await IssueTokenAsync(UserRole.Admin);

        // Act
        HttpResponseMessage response = await CreateClient(token).GetAsync("/api/admin");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
