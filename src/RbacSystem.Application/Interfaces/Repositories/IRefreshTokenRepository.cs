using RbacSystem.Domain.Entities;

namespace RbacSystem.Application.Interfaces.Repositories;

/// <summary>
/// Persistence abstraction for <see cref="RefreshToken"/> records.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Persists a newly issued refresh token.
    /// </summary>
    /// <remarks>
    /// The entity carries only the token's hash; the raw value is never stored.
    /// </remarks>
    /// <param name="refreshToken">The token record to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a refresh-token record from the protected hash of the client-held token.
    /// </summary>
    /// <remarks>
    /// The associated user is included so the refresh use case can validate account
    /// eligibility. Soft-deleted users remain excluded by the configured query filter.
    /// </remarks>
    /// <param name="tokenHash">Deterministic HMAC hash of the submitted raw token.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching token and user, or null when no visible row matches.</returns>
    Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consumes one active refresh token and persists its replacement.
    /// </summary>
    /// <remarks>
    /// The conditional update and replacement insert share one database transaction.
    /// Consequently, concurrent requests cannot both consume the same token, and a
    /// failed replacement insert does not leave the old token consumed.
    /// </remarks>
    /// <param name="currentTokenId">Identifier of the token being consumed.</param>
    /// <param name="replacement">New token record containing only its protected hash.</param>
    /// <param name="nowUtc">Current UTC time used for validation and consumption.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>True only when this call consumed the old token and saved the replacement.</returns>
    Task<bool> TryRotateAsync(
        string currentTokenId,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes one active session identified by its refresh-token hash.
    /// </summary>
    /// <remarks>
    /// Returning false for an unknown, expired, used, or already revoked token makes
    /// it possible for the logout use case to remain idempotent.
    /// </remarks>
    Task<bool> TryRevokeSessionAsync(
        string tokenHash,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every active refresh-token session belonging to one user.
    /// </summary>
    /// <returns>The number of sessions revoked.</returns>
    Task<int> RevokeAllActiveForUserAsync(
        string userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically revokes every unrevoked refresh-token record for a user and
    /// increments the user's token version once.
    /// </summary>
    /// <remarks>
    /// Used when refresh-token reuse indicates possible token theft. The reused
    /// historical token is marked revoked along with active sessions, making repeat
    /// submissions idempotent. Keeping both changes in one transaction prevents
    /// refresh tokens from being revoked while previously issued access tokens
    /// remain valid under the old token version.
    /// </remarks>
    /// <returns>True when the user existed and the security transition committed.</returns>
    Task<bool> RevokeAllSessionsAndIncrementTokenVersionAsync(
        string userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default);
}
