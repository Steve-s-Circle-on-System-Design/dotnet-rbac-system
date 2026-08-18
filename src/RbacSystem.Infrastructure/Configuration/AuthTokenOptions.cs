using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Infrastructure.Configuration;

/// <summary>
/// Token lifetimes bound from the <c>Auth</c> configuration section.
/// </summary>
/// <remarks>
/// Lifetimes are configuration rather than compile-time constants so they can be
/// tuned per environment. Non-secret defaults live in appsettings.json.
/// </remarks>
public sealed class AuthTokenOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Auth";

    /// <summary>Access-token lifetime in minutes.</summary>
    [Range(1, 1440, ErrorMessage = "Auth:AccessTokenExpiryMinutes must be between 1 and 1440.")]
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>Refresh-token lifetime in days.</summary>
    [Range(1, 365, ErrorMessage = "Auth:RefreshTokenExpiryDays must be between 1 and 365.")]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
