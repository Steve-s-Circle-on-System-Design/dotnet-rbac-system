using RbacSystem.Application.Features.Auth.Login;

namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Publishes <see cref="AccountLockedEvent"/> notifications so the account owner can
/// be alerted and the lockout audited.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IUserRegisteredEventPublisher"/>. The transport is still an
/// open decision, so the login flow depends only on this abstraction and the email
/// feature swaps the implementation without touching it.
/// </remarks>
public interface IAccountLockedEventPublisher
{
    /// <summary>
    /// Publishes an account-locked event.
    /// </summary>
    /// <param name="lockedEvent">The event to publish.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PublishAsync(AccountLockedEvent lockedEvent, CancellationToken cancellationToken = default);
}
