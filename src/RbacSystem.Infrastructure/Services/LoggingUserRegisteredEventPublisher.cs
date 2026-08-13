using Microsoft.Extensions.Logging;
using RbacSystem.Application.Features.Auth.Register;
using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Infrastructure.Services;

/// <summary>
/// Records user-registration events in the application log.
/// </summary>
/// <remarks>
/// A placeholder transport until the email feature lands. It exists so that
/// registration can publish its event today without settling the still-open choice
/// between MediatR and a background service.
/// </remarks>
public sealed partial class LoggingUserRegisteredEventPublisher : IUserRegisteredEventPublisher
{
    private readonly ILogger<LoggingUserRegisteredEventPublisher> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingUserRegisteredEventPublisher"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record published events.</param>
    public LoggingUserRegisteredEventPublisher(ILogger<LoggingUserRegisteredEventPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <inheritdoc />
    public Task PublishAsync(
        UserRegisteredEvent registeredEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registeredEvent);

        // The address is masked because registration events land in ordinary
        // application logs, which are a poor place to accumulate personal data.
        LogUserRegistered(
            registeredEvent.UserId,
            registeredEvent.OccurredAtUtc,
            MaskEmail(registeredEvent.Email));

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "User {UserId} registered at {OccurredAtUtc}; verification email pending for {MaskedEmail}.")]
    private partial void LogUserRegistered(string userId, DateTime occurredAtUtc, string maskedEmail);

    /// <summary>
    /// Reduces an address to its first character and domain, e.g. <c>a***@example.com</c>.
    /// </summary>
    private static string MaskEmail(string email)
    {
        int separatorIndex = email.IndexOf('@');

        return separatorIndex <= 0
            ? "***"
            : $"{email[0]}***{email[separatorIndex..]}";
    }
}
