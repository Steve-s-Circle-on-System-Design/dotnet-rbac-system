using Microsoft.Extensions.Logging;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Infrastructure.Services;

/// <summary>
/// Records account-lockout alerts in the application log.
/// </summary>
/// <remarks>
/// A temporary placeholder in the same shape as
/// <see cref="LoggingUserRegisteredEventPublisher"/>: it sends no email. It exists so
/// lockout can raise its security event today without settling the still-open choice
/// of transport, and <strong>must be replaced when email delivery is implemented</strong>.
/// </remarks>
public sealed partial class LoggingAccountLockedEventPublisher : IAccountLockedEventPublisher
{
    private readonly ILogger<LoggingAccountLockedEventPublisher> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingAccountLockedEventPublisher"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record published events.</param>
    public LoggingAccountLockedEventPublisher(ILogger<LoggingAccountLockedEventPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <inheritdoc />
    public Task PublishAsync(AccountLockedEvent lockedEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lockedEvent);

        // Logged at Warning because a lockout is a security signal worth surfacing,
        // and with the address masked because these land in ordinary application
        // logs, which are a poor place to accumulate personal data.
        LogAccountLocked(
            lockedEvent.UserId,
            lockedEvent.FailedAttempts,
            lockedEvent.LockedUntilUtc,
            MaskEmail(lockedEvent.Email));

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Account {UserId} locked after {FailedAttempts} failed sign-in attempts, until {LockedUntilUtc}; security alert pending for {MaskedEmail}.")]
    private partial void LogAccountLocked(
        string userId,
        int failedAttempts,
        DateTime lockedUntilUtc,
        string maskedEmail);

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
