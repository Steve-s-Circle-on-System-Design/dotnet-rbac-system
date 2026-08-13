using RbacSystem.Application.Features.Auth.Register;
using RbacSystem.Infrastructure.Services;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Infrastructure;

/// <summary>
/// Tests the placeholder registration-event transport, focusing on what it writes to
/// the log.
/// </summary>
public class LoggingUserRegisteredEventPublisherTests
{
    private readonly CapturingLogger<LoggingUserRegisteredEventPublisher> logger = new();

    private LoggingUserRegisteredEventPublisher CreatePublisher()
    {
        return new LoggingUserRegisteredEventPublisher(logger);
    }

    [Fact]
    public async Task PublishAsync_LogsUserIdAndMaskedEmail()
    {
        UserRegisteredEvent registeredEvent = new("user-123", "ada@example.com", DateTime.UtcNow);

        await CreatePublisher().PublishAsync(registeredEvent);

        string message = Assert.Single(logger.Messages);

        Assert.Contains("user-123", message, StringComparison.Ordinal);
        Assert.Contains("a***@example.com", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishAsync_DoesNotLogFullEmailAddress()
    {
        UserRegisteredEvent registeredEvent = new("user-123", "ada.lovelace@example.com", DateTime.UtcNow);

        await CreatePublisher().PublishAsync(registeredEvent);

        // Registration events reach ordinary application logs, so the local part
        // must not be recorded in full.
        Assert.DoesNotContain("ada.lovelace@example.com", Assert.Single(logger.Messages), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishAsync_Throws_WhenEventIsNull()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreatePublisher().PublishAsync(null!));
    }

    [Fact]
    public void Constructor_Throws_WhenLoggerIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new LoggingUserRegisteredEventPublisher(null!));
    }
}
