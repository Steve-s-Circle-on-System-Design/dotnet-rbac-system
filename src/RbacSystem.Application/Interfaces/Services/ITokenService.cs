using System.Net;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Issues access and refresh token pairs.
/// </summary>
/// <remarks>
/// Shared by login and, once refresh-token rotation is implemented, by the refresh
/// endpoint: rotation calls the same method with the existing token family and the
/// identifier of the token being replaced.
/// </remarks>
public interface ITokenService
{
    /// <summary>
    /// Signs an access token and issues a matching refresh token, persisting only
    /// the refresh token's hash.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="tokenFamily">
    /// Identifier shared by every refresh token in one login session. Pass a new
    /// value for a fresh login, or the existing family when rotating.
    /// </param>
    /// <param name="userAgent">Calling user agent, recorded for session auditing.</param>
    /// <param name="ipAddress">Calling IP address, recorded for session auditing.</param>
    /// <param name="rotatedFromId">
    /// Identifier of the refresh token being replaced, when rotating. Null on login.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The issued pair, including the raw refresh token, which is never stored.</returns>
    Task<IssuedTokens> IssueTokenPairAsync(
        User user,
        string tokenFamily,
        string? userAgent,
        IPAddress? ipAddress,
        string? rotatedFromId = null,
        CancellationToken cancellationToken = default);
}
