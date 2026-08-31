namespace RbacSystem.Application.Interfaces.Repositories;

/// <summary>
/// Result of recording one failed login attempt.
/// </summary>
/// <param name="FailedAttempts">
/// The consecutive failure count after this attempt. Zero when the attempt was not
/// counted because a lockout was already active.
/// </param>
/// <param name="LockoutEnd">When the account unlocks, or null when it is not locked.</param>
/// <param name="LockoutJustStarted">
/// Whether <em>this</em> attempt is the one that started the lockout.
/// </param>
/// <remarks>
/// <paramref name="LockoutJustStarted"/> is what makes "exactly one alert per
/// lockout" correct rather than merely likely: it is decided by the same atomic
/// statement that applies the lock, so concurrent attempts cannot both believe they
/// were the one that tripped it.
/// </remarks>
public sealed record FailedLoginOutcome(
    int FailedAttempts,
    DateTime? LockoutEnd,
    bool LockoutJustStarted)
{
    /// <summary>
    /// The outcome when a lockout was already active, so nothing was recorded.
    /// </summary>
    public static FailedLoginOutcome AlreadyLocked(DateTime? lockoutEnd)
    {
        return new FailedLoginOutcome(0, lockoutEnd, false);
    }
}
