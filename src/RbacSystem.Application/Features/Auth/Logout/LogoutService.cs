using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Application.Features.Auth.Logout;

/// <inheritdoc cref="ILogoutService" />
public sealed class LogoutService(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    TimeProvider timeProvider) : ILogoutService
{
    private const string logoutReason = "logout";

    /// <inheritdoc />
    public async Task LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        string tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Deliberately ignored the result. Active, expired, unknown and previously
        // revoked tokens all produce the same successful public logout response.
        _ = await refreshTokenRepository.TryRevokeSessionAsync(
            tokenHash,
            nowUtc,
            logoutReason,
            cancellationToken);
    }
}
