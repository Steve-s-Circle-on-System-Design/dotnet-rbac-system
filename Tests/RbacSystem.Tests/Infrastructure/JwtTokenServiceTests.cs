using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;

namespace RbacSystem.Tests.Infrastructure;

/// <summary>
/// Tests access-token claims and prepared refresh-token records.
/// </summary>
public class JwtTokenServiceTests
{
    private const string signingKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";
    private const string hashSecret = "test-refresh-token-hash-secret";
    private const string tokenFamily = "11111111-1111-1111-1111-111111111111";

    private static readonly DateTime now = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider timeProvider = new(now);

    private static JwtOptions Jwt()
    {
        return new JwtOptions
        {
            Issuer = "RbacSystem",
            Audience = "RbacSystemUsers",
            Key = signingKey,
            RefreshTokenHashSecret = hashSecret
        };
    }

    private JwtTokenService CreateService(int accessMinutes = 15, int refreshDays = 7)
    {
        AuthTokenOptions lifetimes = new()
        {
            AccessTokenExpiryMinutes = accessMinutes,
            RefreshTokenExpiryDays = refreshDays
        };

        return new JwtTokenService(
            Options.Create(Jwt()),
            Options.Create(lifetimes),
            new RefreshTokenHasher(Options.Create(Jwt())),
            timeProvider);
    }

    private static User User(UserRole role = UserRole.User, int tokenVersion = 3)
    {
        return new User
        {
            Email = "ada@example.com",
            Name = "ada",
            PasswordHash = "$2a$12$hash",
            Role = role,
            TokenVersion = tokenVersion
        };
    }

    private static JsonWebToken Parse(string accessToken)
    {
        return new JsonWebTokenHandler().ReadJsonWebToken(accessToken);
    }

    /// <summary>
    /// Decodes the raw JWT payload into claim names and values.
    /// </summary>
    /// <remarks>
    /// Reads the encoded segment directly rather than going through the token
    /// object, because the handler's claim-type mapping can rewrite names on the way
    /// in or out. Only the bytes actually on the wire prove what a sibling service
    /// would receive.
    /// </remarks>
    private static Dictionary<string, string> RawPayload(string accessToken)
    {
        byte[] json = Base64UrlDecode(accessToken.Split('.')[1]);

        using var document = JsonDocument.Parse(json);

        return document.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
    }

    [Fact]
    public void IssueTokenPair_IncludesEverySixRequiredClaim()
    {
        User user = User();

        IssuedTokens tokens = CreateService().IssueTokenPair(user, tokenFamily, null, null).Tokens;
        Dictionary<string, string> payload = RawPayload(tokens.AccessToken);

        Assert.Equal(user.Id, payload["sub"]);
        Assert.Equal("ada@example.com", payload["email"]);
        Assert.Equal("user", payload[JwtTokenService.RoleClaim]);
        Assert.Equal(tokenFamily, payload["sid"]);
        Assert.True(Guid.TryParse(payload["jti"], out _));
        Assert.Equal("3", payload[JwtTokenService.TokenVersionClaim]);
    }

    [Fact]
    public void IssueTokenPair_EmitsAdminRoleInLowercase()
    {
        IssuedTokens tokens = CreateService().IssueTokenPair(User(UserRole.Admin), tokenFamily, null, null).Tokens;

        Assert.Equal("admin", RawPayload(tokens.AccessToken)[JwtTokenService.RoleClaim]);
    }

    [Fact]
    public void IssueTokenPair_UsesTheShortRoleClaimName_NotTheSchemaUri()
    {
        // Supplying claims as a ClaimsIdentity puts them through outbound claim-type
        // mapping, which rewrites "role" to the long schema URI. This asserts on the
        // encoded payload so that regression cannot slip past again.
        IssuedTokens tokens = CreateService().IssueTokenPair(User(), tokenFamily, null, null).Tokens;
        Dictionary<string, string> payload = RawPayload(tokens.AccessToken);

        Assert.Equal("role", JwtTokenService.RoleClaim);
        Assert.True(payload.ContainsKey("role"));
        Assert.DoesNotContain(payload.Keys, name => name.StartsWith("http://schemas", StringComparison.Ordinal));
    }

    [Fact]
    public void IssueTokenPair_SetsIssuerAudienceAndConfiguredExpiry()
    {
        IssuedTokens tokens = CreateService(accessMinutes: 15).IssueTokenPair(User(), tokenFamily, null, null).Tokens;
        JsonWebToken jwt = Parse(tokens.AccessToken);

        Assert.Equal("RbacSystem", jwt.Issuer);
        Assert.Contains("RbacSystemUsers", jwt.Audiences);
        Assert.Equal(now.AddMinutes(15), jwt.ValidTo);
        Assert.Equal(900, tokens.AccessTokenExpiresInSeconds);
    }

    [Fact]
    public void IssueTokenPair_HonoursANonDefaultAccessLifetime()
    {
        IssuedTokens tokens = CreateService(accessMinutes: 5).IssueTokenPair(User(), tokenFamily, null, null).Tokens;

        Assert.Equal(now.AddMinutes(5), Parse(tokens.AccessToken).ValidTo);
        Assert.Equal(300, tokens.AccessTokenExpiresInSeconds);
    }

    [Fact]
    public void IssueTokenPair_ProducesA48ByteRandomRefreshToken()
    {
        IssuedTokens tokens = CreateService().IssueTokenPair(User(), tokenFamily, null, null).Tokens;

        byte[] decoded = Base64UrlDecode(tokens.RefreshToken);

        Assert.Equal(48, decoded.Length);
    }

    [Fact]
    public void IssueTokenPair_ProducesADifferentRefreshTokenEachTime()
    {
        JwtTokenService service = CreateService();

        IssuedTokens first = service.IssueTokenPair(User(), tokenFamily, null, null).Tokens;
        IssuedTokens second = service.IssueTokenPair(User(), tokenFamily, null, null).Tokens;

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    }

    [Fact]
    public void IssueTokenPair_ContainsOnlyTheHashedRefreshTokenInItsRecord()
    {
        PreparedTokenPair prepared = CreateService().IssueTokenPair(User(), tokenFamily, null, null);
        IssuedTokens tokens = prepared.Tokens;
        RefreshToken stored = prepared.RefreshTokenRecord;

        Assert.NotEqual(tokens.RefreshToken, stored.TokenHash);
        Assert.DoesNotContain(tokens.RefreshToken, stored.TokenHash, StringComparison.Ordinal);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.Matches("^[0-9a-f]{64}$", stored.TokenHash);
    }

    [Fact]
    public void IssueTokenPair_HashesWithKeyedHmac_NotABarePlainDigest()
    {
        PreparedTokenPair prepared = CreateService().IssueTokenPair(User(), tokenFamily, null, null);
        IssuedTokens tokens = prepared.Tokens;
        RefreshToken stored = prepared.RefreshTokenRecord;

        string plainSha256 = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken))).ToLowerInvariant();

        // A bare digest would let anyone holding the database match intercepted
        // tokens without needing the server secret.
        Assert.NotEqual(plainSha256, stored.TokenHash);

        string expected = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(hashSecret), Encoding.UTF8.GetBytes(tokens.RefreshToken)))
            .ToLowerInvariant();

        Assert.Equal(expected, stored.TokenHash);
    }

    [Fact]
    public void IssueTokenPair_PreparesSessionMetadata()
    {
        var address = IPAddress.Parse("203.0.113.7");

        RefreshToken stored = CreateService(refreshDays: 7)
            .IssueTokenPair(User(), tokenFamily, "curl/8.0", address)
            .RefreshTokenRecord;

        Assert.Equal(tokenFamily, stored.TokenFamily);
        Assert.Equal(now.AddDays(7), stored.ExpiresAt);
        Assert.Equal(now, stored.CreatedAt);
        Assert.Equal("curl/8.0", stored.UserAgent);
        Assert.Equal(address, stored.IpAddress);
        Assert.Null(stored.RotatedFromId);
        Assert.Null(stored.UsedAt);
        Assert.Null(stored.RevokedAt);
    }

    [Fact]
    public void IssueTokenPair_RecordsTheRotationSourceWhenSupplied()
    {
        // Rotation is implemented by a later issue, but the parameter has to thread
        // through correctly for that work to build on this.
        PreparedTokenPair prepared = CreateService()
            .IssueTokenPair(User(), tokenFamily, null, null, "previous-token-id");

        Assert.Equal("previous-token-id", prepared.RefreshTokenRecord.RotatedFromId);
    }

    [Fact]
    public void IssueTokenPair_TruncatesAnOverlongUserAgent()
    {
        // user_agent is varchar(500); an oversized header must not break the insert.
        PreparedTokenPair prepared = CreateService()
            .IssueTokenPair(User(), tokenFamily, new string('x', 600), null);

        Assert.Equal(500, prepared.RefreshTokenRecord.UserAgent!.Length);
    }

    [Fact]
    public void IssueTokenPair_Throws_ForMissingArguments()
    {
        JwtTokenService service = CreateService();

        _ = Assert.Throws<ArgumentNullException>(
            () => service.IssueTokenPair(null!, tokenFamily, null, null));
        _ = Assert.ThrowsAny<ArgumentException>(
            () => service.IssueTokenPair(User(), "  ", null, null));
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');

        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };

        return Convert.FromBase64String(padded);
    }
}
