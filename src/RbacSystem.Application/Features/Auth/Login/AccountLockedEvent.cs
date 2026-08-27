namespace RbacSystem.Application.Features.Auth.Login;

/// <summary>
/// Raised once when repeated failed logins lock an account, so a security alert can
/// be sent and the event audited.
/// </summary>
/// <param name="UserId">Identifier of the locked account.</param>
/// <param name="Email">The account's normalized email address.</param>
/// <param name="FailedAttempts">Consecutive failures that triggered the lockout.</param>
/// <param name="LockedUntilUtc">When the account unlocks, in UTC.</param>
/// <param name="OccurredAtUtc">When the lockout started, in UTC.</param>
public sealed record AccountLockedEvent(
    string UserId,
    string Email,
    int FailedAttempts,
    DateTime LockedUntilUtc,
    DateTime OccurredAtUtc);
