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
}
