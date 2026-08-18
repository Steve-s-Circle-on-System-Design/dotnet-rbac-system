using System.Net;

namespace RbacSystem.Application.Features.Auth.Login;

/// <summary>
/// Authenticates a credential pair and issues the initial token pair.
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// Verifies the supplied credentials and, on success, starts an authenticated session.
    /// </summary>
    /// <param name="request">The validated login request.</param>
    /// <param name="userAgent">Calling user agent, recorded against the session.</param>
    /// <param name="ipAddress">Calling IP address, recorded against the session.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The outcome, with tokens when authentication succeeded.</returns>
    Task<LoginResult> LoginAsync(
        LoginRequest request,
        string? userAgent = null,
        IPAddress? ipAddress = null,
        CancellationToken cancellationToken = default);
}
