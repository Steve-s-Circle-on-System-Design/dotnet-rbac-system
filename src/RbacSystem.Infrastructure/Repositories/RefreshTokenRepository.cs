using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Domain.Entities;
using RbacSystem.Infrastructure.Persistence;

namespace RbacSystem.Infrastructure.Repositories;

/// <inheritdoc cref="IRefreshTokenRepository" />
public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    /// <inheritdoc />
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        // Tracked only; the caller commits, so the new session row and the user's
        // last-login update land in a single transaction.
        _ = await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        // No tracking prevents a row read before rotation from later overwriting the
        // atomic database update with stale UsedAt or RevokedAt values.
        //return context.RefreshTokens.FirstOrDefaultAsync(k => k.TokenHash == tokenHash, cancellationToken);
        return context.RefreshTokens
            .AsNoTracking()
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryRotateAsync(
        string currentTokenId,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTokenId);
        ArgumentNullException.ThrowIfNull(replacement);
        EnsureUtc(nowUtc);

        if (!string.Equals(replacement.RotatedFromId, currentTokenId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The replacement token must reference the token it rotates.", nameof(replacement));
        }

        await using IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            int consumed = await context.RefreshTokens
                .Where(token =>
                    token.Id == currentTokenId &&
                    token.UserId == replacement.UserId &&
                    token.TokenFamily == replacement.TokenFamily &&
                    // Re-enable once email verification promotes registrations to
                    // Active. Login currently permits a verified user whose status
                    // remains PendingVerification, so rotation must match that rule.
                    // token.User.Status == UserStatus.Active &&
                    token.UsedAt == null &&
                    token.RevokedAt == null &&
                    token.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(token => token.UsedAt, nowUtc),
                    cancellationToken);

            if (consumed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            _ = await context.RefreshTokens.AddAsync(replacement, cancellationToken);
            _ = await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryRevokeSessionAsync(
        string tokenHash,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ValidateRevocation(nowUtc, reason);

        int revoked = await context.RefreshTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.UsedAt == null &&
                token.RevokedAt == null &&
                token.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(token => token.RevokedAt, nowUtc)
                    .SetProperty(token => token.RevokeReason, reason),
                cancellationToken);

        return revoked == 1;
    }

    /// <inheritdoc />
    public Task<int> RevokeAllActiveForUserAsync(
        string userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ValidateRevocation(nowUtc, reason);

        return context.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.UsedAt == null &&
                token.RevokedAt == null &&
                token.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(token => token.RevokedAt, nowUtc)
                    .SetProperty(token => token.RevokeReason, reason),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAllSessionsAndIncrementTokenVersionAsync(
        string userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ValidateRevocation(nowUtc, reason);

        await using IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Include the already-used token that revealed the reuse. Marking every
            // unrevoked record makes this security response idempotent: a repeated
            // submission finds that token revoked and cannot keep incrementing the
            // user's token version after they sign in again.
            int revoked = await context.RefreshTokens
                .IgnoreQueryFilters()
                .Where(token =>
                    token.UserId == userId &&
                    token.RevokedAt == null)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(token => token.RevokedAt, nowUtc)
                        .SetProperty(token => token.RevokeReason, reason),
                    cancellationToken);

            if (revoked == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            int versionUpdated = await context.Users
                .IgnoreQueryFilters()
                .Where(user => user.Id == userId)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(user => user.TokenVersion, user => user.TokenVersion + 1)
                        .SetProperty(user => user.UpdatedAt, nowUtc),
                    cancellationToken);

            if (versionUpdated != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void ValidateRevocation(DateTime nowUtc, string reason)
    {
        EnsureUtc(nowUtc);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (reason.Length > 255)
        {
            throw new ArgumentException(
                "The revocation reason must not exceed 255 characters.", nameof(reason));
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"The supplied time must be UTC, but was {value.Kind}.", nameof(value));
        }
    }
}
