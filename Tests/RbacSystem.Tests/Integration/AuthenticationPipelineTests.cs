using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Integration;

/// <summary>
/// Drives a token produced by <see cref="JwtTokenService"/> through the real
/// authentication middleware.
/// </summary>
/// <remarks>
/// These exist because a unit test asserting on the issued token cannot see what
/// validation does to it. Inbound claim mapping silently rewrote the role claim
/// during validation, and only an end-to-end check through the pipeline catches it.
/// </remarks>
public sealed class AuthenticationPipelineTests(WebApplicationFactoryFixture fixture)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private static async Task<string> IssueTokenAsync(UserRole role)
    {
        JwtOptions jwt = new()
        {
            Issuer = AuthApiFactory.Issuer,
            Audience = AuthApiFactory.Audience,
            Key = AuthApiFactory.SigningKey,
            RefreshTokenHashSecret = "integration-test-refresh-hash-secret-32-chars"
        };

        JwtTokenService tokenService = new(
            Options.Create(jwt),
            Options.Create(new AuthTokenOptions()),
            new FakeRefreshTokenRepository(),
            new RefreshTokenHasher(Options.Create(jwt)),
            new FakeTimeProvider(DateTimeOffset.UtcNow));

        User user = new()
        {
            Email = "ada@example.com",
            Name = "ada",
            PasswordHash = "$2a$12$hash",
            Role = role
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
    public async Task ProtectedEndpoint_Rejects_WhenNoTokenIsSupplied()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/test-probe/authenticated");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_Rejects_AGarbageToken()
    {
        HttpResponseMessage response = await CreateClient("not-a-real-token").GetAsync("/test-probe/authenticated");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_Accepts_AnIssuedToken()
    {
        HttpResponseMessage response = await CreateClient(await IssueTokenAsync(UserRole.User))
            .GetAsync("/test-probe/authenticated");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleClaim_SurvivesValidation_UnderItsShortName()
    {
        // The regression guard. With inbound claim mapping left on, "role" is
        // rewritten to the schema URI during validation, RoleClaimType stops
        // resolving, and IsInRole silently returns false for everyone.
        HttpResponseMessage response = await CreateClient(await IssueTokenAsync(UserRole.User))
            .GetAsync("/test-probe/authenticated");

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("user", body.GetProperty("role").GetString());
        Assert.True(body.GetProperty("isInUserRole").GetBoolean());
        Assert.False(body.GetProperty("isInAdminRole").GetBoolean());
    }

    [Fact]
    public async Task SubjectClaim_SurvivesValidation_AsTheIdentityName()
    {
        HttpResponseMessage response = await CreateClient(await IssueTokenAsync(UserRole.User))
            .GetAsync("/test-probe/authenticated");

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(Guid.TryParse(body.GetProperty("subject").GetString(), out _));
    }

    [Fact]
    public async Task RoleAuthorization_AllowsTheMatchingRole()
    {
        HttpResponseMessage response = await CreateClient(await IssueTokenAsync(UserRole.User))
            .GetAsync("/test-probe/user-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_ForbidsARoleTheUserDoesNotHold()
    {
        HttpResponseMessage response = await CreateClient(await IssueTokenAsync(UserRole.User))
            .GetAsync("/test-probe/admin-only");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_AdmitsAnAdminToken()
    {
        HttpResponseMessage response = await CreateClient(await IssueTokenAsync(UserRole.Admin))
            .GetAsync("/test-probe/admin-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

/// <summary>
/// Shares one hosted application across the pipeline tests, since standing it up is
/// the expensive part.
/// </summary>
public sealed class WebApplicationFactoryFixture : IDisposable
{
    internal AuthApiFactory Factory { get; } = new();

    public void Dispose()
    {
        Factory.Dispose();
    }
}
