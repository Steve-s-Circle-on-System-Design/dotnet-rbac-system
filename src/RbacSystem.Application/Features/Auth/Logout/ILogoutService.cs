namespace RbacSystem.Application.Features.Auth.Logout;

/// <summary>
/// Revokes one refresh-token session without affecting the user's other sessions.
/// </summary>
public interface ILogoutService
{
    /// <summary>
    /// Revokes the session identified by the submitted refresh token. The operation
    /// is idempotent and does not reveal whether the token existed.
    /// </summary>
    /// <param name="request">The validated logout request.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default);
}
