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

    /// <inheritdoc />
    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Added.Add(refreshToken);

        return Task.CompletedTask;
    }
}
