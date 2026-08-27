using RbacSystem.Domain.Entities;

namespace RbacSystem.Application.Interfaces.Repositories;

/// <summary>
/// Persistence abstraction for <see cref="User"/> records.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Determines whether a user already exists for the supplied email address.
    /// </summary>
    /// <param name="email">A normalized (trimmed, lowercase) email address.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the email is already registered.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to persist a new user.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> rather than throwing when the unique email
    /// constraint rejects the insert, so that two concurrent registrations for the
    /// same address produce the same outcome as a sequential duplicate.
    /// </remarks>
    /// <param name="user">The user to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the user was stored.</returns>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the user registered against the supplied email address.
    /// </summary>
    /// <param name="email">A normalized (trimmed, lowercase) email address.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The user, or <see langword="null"/> when the address is not registered.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to tracked users.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one failed login attempt and applies the lockout policy atomically.
    /// </summary>
    /// <remarks>
    /// Deliberately not a read-modify-write: the count, the expiry reset and the
    /// lock decision all happen inside a single statement, so simultaneous attempts
    /// cannot lose increments or each conclude that they were the one that tripped
    /// the lock. An attempt made while a lockout is already active records nothing,
    /// which is what stops a lockout being extended indefinitely.
    /// </remarks>
    /// <param name="userId">The account that failed authentication.</param>
    /// <param name="maxFailedAttempts">Consecutive failures that trigger a lockout.</param>
    /// <param name="lockoutDuration">How long the lockout lasts once triggered.</param>
    /// <param name="nowUtc">Current UTC time, supplied so the policy is testable.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resulting count and lockout state.</returns>
    Task<FailedLoginOutcome> RegisterFailedLoginAsync(
        string userId,
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
