using Microsoft.Extensions.Time.Testing;
using RbacSystem.Application.Features.Auth.Logout;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Features.Auth.Logout;

/// <summary>Behavioural tests for idempotent single-session logout.</summary>
public sealed class LogoutServiceTests
{
    private const string rawToken = "raw-current-refresh-token";
    private static readonly DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeRefreshTokenRepository refreshTokens = new();
    private readonly PrefixRefreshTokenHasher tokenHasher = new();
    private readonly FakeTimeProvider timeProvider = new(now);

    private LogoutService CreateService()
    {
        return new LogoutService(refreshTokens, tokenHasher, timeProvider);
    }

    private RefreshToken SeedActiveToken(string rawValue = rawToken)
    {
        RefreshToken token = new()
        {
            UserId = "user-1",
            TokenHash = tokenHasher.Hash(rawValue),
            TokenFamily = "session-1",
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddHours(-1)
        };

        refreshTokens.Added.Add(token);
        tokenHasher.Arguments.Clear();
        return token;
    }

    [Fact]
    public async Task LogoutAsync_RevokesOnlyTheSubmittedSession()
    {
        RefreshToken current = SeedActiveToken();
        RefreshToken another = SeedActiveToken("another-raw-token");

        await CreateService().LogoutAsync(new LogoutRequest { RefreshToken = rawToken });

        Assert.Equal(now, current.RevokedAt);
        Assert.Equal("logout", current.RevokeReason);
        Assert.Null(another.RevokedAt);
    }

    [Fact]
    public async Task LogoutAsync_IsIdempotentWhenRepeated()
    {
        RefreshToken current = SeedActiveToken();
        LogoutService service = CreateService();
        LogoutRequest request = new() { RefreshToken = rawToken };

        await service.LogoutAsync(request);
        DateTime? firstRevokedAt = current.RevokedAt;
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        await service.LogoutAsync(request);

        Assert.Equal(firstRevokedAt, current.RevokedAt);
        Assert.Equal(2, refreshTokens.RevokeSessionCalls.Count);
    }

    [Fact]
    public async Task LogoutAsync_CompletesForAnUnknownToken()
    {
        await CreateService().LogoutAsync(new LogoutRequest { RefreshToken = "unknown" });

        _ = Assert.Single(refreshTokens.RevokeSessionCalls);
        Assert.Empty(refreshTokens.Added);
    }

    [Fact]
    public async Task LogoutAsync_ForwardsHashTimeReasonAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateService().LogoutAsync(
            new LogoutRequest { RefreshToken = rawToken },
            cancellation.Token);

        (string hash, DateTime when, string reason, CancellationToken token) =
            Assert.Single(refreshTokens.RevokeSessionCalls);

        Assert.Equal($"hash:{rawToken}", hash);
        Assert.Equal(now, when);
        Assert.Equal("logout", reason);
        Assert.Equal(cancellation.Token, token);
    }

    [Fact]
    public async Task LogoutAsync_IgnoresWhitespaceWithoutHashingOrCallingTheRepository()
    {
        await CreateService().LogoutAsync(new LogoutRequest { RefreshToken = "   " });

        Assert.Empty(tokenHasher.Arguments);
        Assert.Empty(refreshTokens.RevokeSessionCalls);
    }

    private sealed class PrefixRefreshTokenHasher : IRefreshTokenHasher
    {
        public List<string> Arguments { get; } = [];

        public string Hash(string rawToken)
        {
            Arguments.Add(rawToken);
            return $"hash:{rawToken}";
        }
    }
}
