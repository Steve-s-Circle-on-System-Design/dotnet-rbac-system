using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Captures account-lockout alerts so tests can assert exactly one is raised per
/// lockout, and none for attempts made while a lockout is already active.
/// </summary>
internal sealed class RecordingAccountLockedEventPublisher : IAccountLockedEventPublisher
{
    /// <summary>Events published, in order.</summary>
    public List<AccountLockedEvent> PublishedEvents { get; } = [];

    /// <inheritdoc />
    public Task PublishAsync(AccountLockedEvent lockedEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(lockedEvent);

        return Task.CompletedTask;
    }
}
