using Microsoft.EntityFrameworkCore;
using RbacSystem.Domain.Entities;
using RbacSystem.Infrastructure.Persistence;
using RbacSystem.Infrastructure.Repositories;

namespace RbacSystem.Tests.Database;

/// <summary>
/// PostgreSQL coverage for the atomic response to refresh-token reuse.
/// </summary>
public sealed class RefreshTokenRevocationTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static readonly DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    [RequiresPostgresFact]
    public async Task ConcurrentReuseResponses_RevokeOnceAndIncrementTokenVersionOnce()
    {
        _ = fixture;
        User user = await PostgresFixture.SeedUserAsync();
        string tokenId = await SeedRefreshTokenAsync(user.Id);

        await using AppDbContext firstContext = PostgresFixture.CreateContext();
        await using AppDbContext secondContext = PostgresFixture.CreateContext();
        var firstRepository = new RefreshTokenRepository(firstContext);
        var secondRepository = new RefreshTokenRepository(secondContext);

        Task<bool> first = firstRepository.RevokeAllSessionsAndIncrementTokenVersionAsync(
            user.Id,
            now,
            "refresh_token_reuse");
        Task<bool> second = secondRepository.RevokeAllSessionsAndIncrementTokenVersionAsync(
            user.Id,
            now,
            "refresh_token_reuse");

        bool[] results = await Task.WhenAll(first, second);

        _ = Assert.Single(results, result => result);
        _ = Assert.Single(results, result => !result);

        User reloaded = await PostgresFixture.ReloadAsync(user.Id);
        Assert.Equal(1, reloaded.TokenVersion);

        await using AppDbContext verificationContext = PostgresFixture.CreateContext();
        RefreshToken token = await verificationContext.RefreshTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == tokenId);

        Assert.Equal(now, token.RevokedAt);
        Assert.Equal("refresh_token_reuse", token.RevokeReason);
    }

    private static async Task<string> SeedRefreshTokenAsync(string userId)
    {
        await using AppDbContext context = PostgresFixture.CreateContext();

        RefreshToken token = new()
        {
            UserId = userId,
            TokenHash = $"hash-{Guid.NewGuid():N}",
            TokenFamily = Guid.NewGuid().ToString(),
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddHours(-1)
        };

        _ = context.RefreshTokens.Add(token);
        _ = await context.SaveChangesAsync();

        return token.Id;
    }
}
