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
}
