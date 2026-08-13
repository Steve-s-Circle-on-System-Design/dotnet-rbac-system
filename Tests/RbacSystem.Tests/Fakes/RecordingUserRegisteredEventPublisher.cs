using RbacSystem.Application.Features.Auth.Register;
using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Captures published registration events so tests can assert they fire exactly
/// once, and only after a successful insert.
/// </summary>
internal sealed class RecordingUserRegisteredEventPublisher : IUserRegisteredEventPublisher
{
    /// <summary>Events published, in order.</summary>
    public List<UserRegisteredEvent> PublishedEvents { get; } = [];

    /// <inheritdoc />
    public Task PublishAsync(
        UserRegisteredEvent registeredEvent,
        CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(registeredEvent);

        return Task.CompletedTask;
    }
}
