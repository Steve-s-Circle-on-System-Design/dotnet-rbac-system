using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Application.Features.Auth.Login;

/// <summary>
/// Payload for <c>POST /api/auth/login</c>.
/// </summary>
/// <remarks>
/// Deliberately a class rather than a record: MVC logs bound action arguments at
/// Information level via <c>ToString()</c>, and a record's synthesized
/// <c>ToString()</c> would write the plain-text password into the application log.
/// </remarks>
public sealed class LoginRequest
{
    /// <summary>
    /// The registered email address. Matched case-insensitively.
    /// </summary>
    /// <example>ada@example.com</example>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The plain-text password. Never stored or logged.
    /// </summary>
    /// <remarks>
    /// Only presence is validated. The registration password policy is intentionally
    /// not applied here: a wrong password must produce an authentication failure
    /// rather than a validation error that would disclose the policy, and an account
    /// created under an older policy must still be able to sign in.
    /// </remarks>
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; init; } = string.Empty;
}
