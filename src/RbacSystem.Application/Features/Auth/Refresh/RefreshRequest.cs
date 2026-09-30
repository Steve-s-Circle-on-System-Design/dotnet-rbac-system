using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Application.Features.Auth.Refresh;

/// <summary>
/// Payload for <c>POST /api/auth/refresh</c>.
/// </summary>
/// <remarks>
/// Deliberately a class rather than a record. A record's generated
/// <c>ToString()</c> would include the raw refresh token, which must never be
/// written to application logs.
/// </remarks>
public sealed class RefreshRequest
{
    /// <summary>
    /// The current session's raw refresh token. It is verified against a stored
    /// HMAC hash and is never stored or logged in plain text.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required.")]
    [StringLength(512, ErrorMessage = "Refresh token must not exceed 512 characters.")]
    public string RefreshToken { get; init; } = string.Empty;
}
