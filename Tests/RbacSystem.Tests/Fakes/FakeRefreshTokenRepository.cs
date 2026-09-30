using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Captures refresh tokens handed to persistence, so tests can assert that only the
/// hash is ever stored.
/// </summary>
internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    /// <summary>Tokens passed to <see cref="AddAsync"/>, in order.</summary>
    public List<RefreshToken> Added { get; } = [];

    /// <summary>Forces the next rotation to lose its conditional-update race.</summary>
    public bool RejectNextRotation { get; set; }

    /// <summary>Reuse-response calls received by the repository.</summary>
    public List<(string UserId, DateTime NowUtc, string Reason)> ReuseResponses { get; } = [];

    /// <summary>Single-session revocation calls received by the repository.</summary>
    public List<(string TokenHash, DateTime NowUtc, string Reason, CancellationToken Token)> RevokeSessionCalls
    {
        get;
    } = [];

    /// <inheritdoc />
    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Added.Add(refreshToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        RefreshToken? token = Added.SingleOrDefault(token => token.TokenHash == tokenHash);

        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<bool> TryRotateAsync(
        string currentTokenId,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (RejectNextRotation)
        {
            RejectNextRotation = false;
            return Task.FromResult(false);
        }

        RefreshToken? current = Added.SingleOrDefault(token => token.Id == currentTokenId);

        if (current is null ||
            current.UserId != replacement.UserId ||
            current.TokenFamily != replacement.TokenFamily ||
            current.UsedAt is not null ||
            current.RevokedAt is not null ||
            current.ExpiresAt <= nowUtc)
        {
            return Task.FromResult(false);
        }

        current.UsedAt = nowUtc;
        Added.Add(replacement);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> TryRevokeSessionAsync(
        string tokenHash,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        RevokeSessionCalls.Add((tokenHash, nowUtc, reason, cancellationToken));
        RefreshToken? token = Added.SingleOrDefault(token => token.TokenHash == tokenHash);

        if (token is null ||
            token.UsedAt is not null ||
            token.RevokedAt is not null ||
            token.ExpiresAt <= nowUtc)
        {
            return Task.FromResult(false);
        }

        token.RevokedAt = nowUtc;
        token.RevokeReason = reason;

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<int> RevokeAllActiveForUserAsync(
        string userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        List<RefreshToken> active = [.. Added
            .Where(token =>
                token.UserId == userId &&
                token.UsedAt is null &&
                token.RevokedAt is null &&
                token.ExpiresAt > nowUtc)];

        foreach (RefreshToken token in active)
        {
            token.RevokedAt = nowUtc;
            token.RevokeReason = reason;
        }

        return Task.FromResult(active.Count);
    }

    /// <inheritdoc />
    public Task<bool> RevokeAllSessionsAndIncrementTokenVersionAsync(
        string userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ReuseResponses.Add((userId, nowUtc, reason));

        List<RefreshToken> userTokens = [.. Added.Where(token => token.UserId == userId)];

        List<RefreshToken> unrevoked = [.. userTokens.Where(token => token.RevokedAt is null)];

        if (unrevoked.Count == 0)
        {
            return Task.FromResult(false);
        }

        foreach (RefreshToken token in unrevoked)
        {
            token.RevokedAt = nowUtc;
            token.RevokeReason = reason;
        }

        User? user = userTokens.Select(token => token.User).FirstOrDefault(candidate => candidate is not null);

        if (user is null)
        {
            return Task.FromResult(false);
        }

        user.TokenVersion++;
        user.UpdatedAt = nowUtc;

        return Task.FromResult(true);
    }
}
