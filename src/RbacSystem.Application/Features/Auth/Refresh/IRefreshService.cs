using System.Net;

namespace RbacSystem.Application.Features.Auth.Refresh;

/// <summary>
/// Rotates a valid refresh token and issues a replacement token pair.
/// </summary>
public interface IRefreshService
{
    /// <summary>
    /// Validates and consumes the submitted refresh token, then creates its
    /// single-use replacement when the session remains eligible.
    /// </summary>
    /// <param name="request">The validated refresh request.</param>
    /// <param name="userAgent">Calling user agent recorded against the replacement token.</param>
    /// <param name="ipAddress">Calling IP address recorded for session auditing.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The outcome, with a new token pair only when rotation succeeds.</returns>
    Task<RefreshResult> RefreshAsync(
        RefreshRequest request,
        string? userAgent = null,
        IPAddress? ipAddress = null,
        CancellationToken cancellationToken = default);
}
