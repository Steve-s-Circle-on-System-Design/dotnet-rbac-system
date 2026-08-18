using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RbacSystem.Infrastructure.Configuration;

namespace RbacSystem.Infrastructure.Services;

/// <summary>
/// Hashes refresh tokens for storage using keyed HMAC-SHA256.
/// </summary>
/// <remarks>
/// A refresh token is 48 bytes of cryptographic randomness, so it needs no slow
/// password hash — brute-forcing the value itself is infeasible. HMAC with a server
/// secret is used rather than a bare SHA-256 digest so that a stolen database alone
/// cannot be matched against intercepted tokens without also holding the secret.
/// </remarks>
public sealed class RefreshTokenHasher
{
    private readonly byte[] secret;

    /// <summary>
    /// Initializes the hasher from the configured <see cref="JwtOptions"/>.
    /// </summary>
    public RefreshTokenHasher(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Value.RefreshTokenHashSecret);

        secret = Encoding.UTF8.GetBytes(options.Value.RefreshTokenHashSecret);
    }

    /// <summary>
    /// Returns the lowercase hex HMAC-SHA256 of a raw refresh token.
    /// </summary>
    /// <remarks>
    /// Deterministic by design: the refresh endpoint has to locate a stored row from
    /// the token a client presents, which a salted hash would not allow.
    /// </remarks>
    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawToken);

        byte[] digest = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
