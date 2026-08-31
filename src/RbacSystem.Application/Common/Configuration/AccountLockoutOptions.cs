using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Application.Common.Configuration;

/// <summary>
/// Account-lockout policy bound from the <c>Auth:Lockout</c> configuration section.
/// </summary>
/// <remarks>
/// The threshold and window are configuration rather than constants so they can be
/// tightened after an incident, or loosened in a test environment, without a code
/// change. This lives in the Application layer because how many failures constitute
/// an attack is a use-case rule, not a persistence detail.
/// </remarks>
public sealed class AccountLockoutOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Auth:Lockout";

    /// <summary>
    /// Consecutive failed attempts that trigger a lockout.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Auth:Lockout:MaxFailedAttempts must be between 1 and 100.")]
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// How long an account stays locked once the threshold is reached, in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Auth:Lockout:DurationMinutes must be between 1 and 1440.")]
    public int DurationMinutes { get; set; } = 15;

    /// <summary>The lockout window as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan Duration => TimeSpan.FromMinutes(DurationMinutes);
}
