using System.Net;
using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Application.Features.Auth.Refresh;

/// <inheritdoc cref="IRefreshService" />
public sealed class RefreshService(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IRefreshService
{
    private const string reuseReason = "refresh_token_reuse";

    /// <inheritdoc />
    public async Task<RefreshResult> RefreshAsync(
        RefreshRequest request,
        string? userAgent = null,
        IPAddress? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return RefreshResult.Invalid();
        }

        string tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        RefreshToken? currentToken = await refreshTokenRepository.GetByHashAsync(
            tokenHash,
            cancellationToken);

        if (currentToken is null)
        {
            return RefreshResult.Invalid();
        }

        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (currentToken.RevokedAt is not null)
        {
            return RefreshResult.Invalid();
        }

        if (currentToken.UsedAt is not null)
        {
            await RespondToReuseAsync(currentToken.UserId, nowUtc, cancellationToken);
            return RefreshResult.Invalid();
        }

        if (currentToken.ExpiresAt <= nowUtc ||
            currentToken.User is null ||
            currentToken.User.Status is UserStatus.Inactive or UserStatus.Suspended)
        {
            return RefreshResult.Invalid();
        }

        PreparedTokenPair replacement = tokenService.IssueTokenPair(
            currentToken.User,
            currentToken.TokenFamily,
            userAgent,
            ipAddress,
            currentToken.Id);

        bool rotated = await refreshTokenRepository.TryRotateAsync(
            currentToken.Id,
            replacement.RefreshTokenRecord,
            nowUtc,
            cancellationToken);

        if (!rotated)
        {
            await RespondToReuseAsync(currentToken.UserId, nowUtc, cancellationToken);
            return RefreshResult.Invalid();
        }

        IssuedTokens tokens = replacement.Tokens;

        return RefreshResult.Success(new RefreshResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            "Bearer",
            tokens.AccessTokenExpiresInSeconds));
    }

    private async Task RespondToReuseAsync(
        string userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        _ = await refreshTokenRepository.RevokeAllSessionsAndIncrementTokenVersionAsync(
            userId,
            nowUtc,
            reuseReason,
            cancellationToken);
    }
}
