using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Application.Features.Auth.AccessToken;

/// <inheritdoc cref="IAccessTokenValidator" />
public sealed class AccessTokenValidator(IUserRepository userRepository) : IAccessTokenValidator
{
    /// <inheritdoc />
    public async Task<bool> IsValidAsync(
        string userId,
        int tokenVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || tokenVersion < 0)
        {
            return false;
        }

        // Future scalability improvement: cache this minimal state in Redis by user
        // ID, while keeping PostgreSQL as the source of truth. Every global security
        // change must invalidate or replace the cache entry only after its database
        // transaction commits, otherwise a stale version could accept an old token.
        UserSecurityState? state = await userRepository.GetSecurityStateByIdAsync(
            userId,
            cancellationToken);

        if (state is null ||
            state.Status is UserStatus.Inactive or UserStatus.Suspended)
        {
            return false;
        }

        // PendingVerification remains allowed until the email-verification flow
        // consistently promotes verified registrations to Active. This matches the
        // temporary rule already used by login and refresh.
        return state.TokenVersion == tokenVersion;
    }
}
