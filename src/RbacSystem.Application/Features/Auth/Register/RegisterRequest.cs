using System.ComponentModel.DataAnnotations;
using RbacSystem.Application.Common.Validation;

namespace RbacSystem.Application.Features.Auth.Register;

/// <summary>
/// Payload for <c>POST /api/auth/register</c>.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// The email address to register. Compared and stored case-insensitively.
    /// </summary>
    /// <example>ada@example.com</example>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The plain-text password. Never stored or logged; only its hash is persisted.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [PasswordPolicy]
    public string Password { get; init; } = string.Empty;
}
