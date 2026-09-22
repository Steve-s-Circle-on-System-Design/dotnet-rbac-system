using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Application.Features.Auth.Logout;

/// <summary>
/// Payload for <c>POST /api/auth/logout</c>.
/// </summary>
/// <remarks>
/// Deliberately a class rather than a record so a generated <c>ToString()</c>
/// cannot expose the raw refresh token in application logs.
/// </remarks>
public sealed class LogoutRequest
{
    /// <summary>
    /// Raw refresh token identifying the session that should be revoked.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required.")]
    [StringLength(512, ErrorMessage = "Refresh token must not exceed 512 characters.")]
    public string RefreshToken { get; init; } = string.Empty;
}
