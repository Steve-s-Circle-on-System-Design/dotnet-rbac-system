using System.Net;
using Microsoft.Extensions.Time.Testing;
using RbacSystem.Application.Features.Auth.Refresh;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Features.Auth.Refresh;

/// <summary>Behavioural tests for refresh-token validation and rotation.</summary>
public sealed class RefreshServiceTests
{
    private const string rawToken = "raw-current-refresh-token";
    private const string tokenFamily = "11111111-1111-1111-1111-111111111111";

    private static readonly DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeRefreshTokenRepository refreshTokens = new();
    private readonly FakeTokenService tokenService = new();
    private readonly FakeTimeProvider timeProvider = new(now);
    private readonly PrefixRefreshTokenHasher tokenHasher = new();

    private RefreshService CreateService()
    {
        return new RefreshService(refreshTokens, tokenHasher, tokenService, timeProvider);
    }

    private RefreshToken SeedToken(
        UserStatus status = UserStatus.Active,
        DateTime? expiresAt = null,
        DateTime? usedAt = null,
        DateTime? revokedAt = null)
    {
        User user = new()
        {
            Email = "ada@example.com",
            Name = "ada",
            Role = UserRole.User,
            Status = status,
            EmailVerifiedAt = now.AddDays(-1),
            TokenVersion = 4
        };

        RefreshToken token = new()
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHasher.Hash(rawToken),
            TokenFamily = tokenFamily,
            ExpiresAt = expiresAt ?? now.AddDays(1),
            UsedAt = usedAt,
            RevokedAt = revokedAt,
            CreatedAt = now.AddDays(-1)
        };

        refreshTokens.Added.Add(token);
        return token;
    }

    [Fact]
    public async Task RefreshAsync_RotatesAValidTokenAndReturnsTheReplacementPair()
    {
        RefreshToken current = SeedToken();
        var address = IPAddress.Parse("203.0.113.7");

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken },
            "curl/8.0",
            address);

        Assert.Equal(RefreshOutcome.Success, result.Outcome);
        Assert.NotNull(result.Response);
        Assert.Equal("access-token", result.Response.AccessToken);
        Assert.Equal("refresh-token", result.Response.RefreshToken);
        Assert.Equal("Bearer", result.Response.TokenType);
        Assert.Equal(900, result.Response.ExpiresIn);
        Assert.Equal(now, current.UsedAt);

        RefreshToken replacement = Assert.Single(refreshTokens.Added, token => token.Id != current.Id);
        Assert.Equal(current.Id, replacement.RotatedFromId);
        Assert.Equal(current.TokenFamily, replacement.TokenFamily);
        Assert.Equal("curl/8.0", replacement.UserAgent);
        Assert.Equal(address, replacement.IpAddress);
        Assert.Empty(refreshTokens.ReuseResponses);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsTheSameFailureForAnUnknownToken()
    {
        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = "unknown" });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Null(result.Response);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task RefreshAsync_RejectsAnExpiredTokenWithoutIssuingAReplacement()
    {
        _ = SeedToken(expiresAt: now);

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Empty(tokenService.Issued);
        Assert.Empty(refreshTokens.ReuseResponses);
    }

    [Theory]
    [InlineData(UserStatus.Inactive)]
    [InlineData(UserStatus.Suspended)]
    public async Task RefreshAsync_RejectsBlockedAccounts(UserStatus status)
    {
        _ = SeedToken(status: status);

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task RefreshAsync_AllowsPendingVerificationUntilStatusPromotionIsImplemented()
    {
        _ = SeedToken(status: UserStatus.PendingVerification);

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken });

        Assert.Equal(RefreshOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task RefreshAsync_RespondsToUsedTokenReuseByRevokingSessionsAndIncrementingVersion()
    {
        RefreshToken reused = SeedToken(usedAt: now.AddMinutes(-1));
        RefreshToken active = new()
        {
            UserId = reused.UserId,
            User = reused.User,
            TokenHash = "another-token-hash",
            TokenFamily = "another-session-family",
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddHours(-1)
        };
        refreshTokens.Added.Add(active);

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Equal(5, reused.User.TokenVersion);
        Assert.Equal(now, reused.RevokedAt);
        Assert.Equal(now, active.RevokedAt);

        (string userId, DateTime when, string reason) = Assert.Single(refreshTokens.ReuseResponses);
        Assert.Equal(reused.UserId, userId);
        Assert.Equal(now, when);
        Assert.Equal("refresh_token_reuse", reason);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task RefreshAsync_DoesNotRepeatTheSecurityTransitionForAnAlreadyRevokedToken()
    {
        RefreshToken token = SeedToken(
            usedAt: now.AddMinutes(-2),
            revokedAt: now.AddMinutes(-1));

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Equal(4, token.User.TokenVersion);
        Assert.Empty(refreshTokens.ReuseResponses);
    }

    [Fact]
    public async Task RefreshAsync_TreatsALostRotationRaceAsReuse()
    {
        RefreshToken current = SeedToken();
        refreshTokens.RejectNextRotation = true;

        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = rawToken });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Null(result.Response);
        Assert.Equal(5, current.User.TokenVersion);
        Assert.Equal(now, current.RevokedAt);
        _ = Assert.Single(refreshTokens.ReuseResponses);
        _ = Assert.Single(tokenService.Issued);
        _ = Assert.Single(refreshTokens.Added);
    }

    [Fact]
    public async Task RefreshAsync_RejectsWhitespaceWithoutCallingTheHasher()
    {
        RefreshResult result = await CreateService().RefreshAsync(
            new RefreshRequest { RefreshToken = "   " });

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
        Assert.Empty(tokenHasher.Arguments);
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
