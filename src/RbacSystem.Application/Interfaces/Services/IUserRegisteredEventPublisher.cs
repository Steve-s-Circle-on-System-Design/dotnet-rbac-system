using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Publishes <see cref="UserRegisteredEvent"/> notifications to whichever transport
/// the email feature adopts.
/// </summary>
/// <remarks>
/// The concrete mechanism (in-process handler, MediatR, or a background queue) is
/// still an open decision, so registration depends only on this abstraction.
/// </remarks>
public interface IUserRegisteredEventPublisher
{
    /// <summary>
    /// Publishes a user-registration event.
    /// </summary>
    /// <param name="registeredEvent">The event to publish.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PublishAsync(UserRegisteredEvent registeredEvent, CancellationToken cancellationToken = default);
}
