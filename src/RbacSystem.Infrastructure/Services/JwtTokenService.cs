using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Common;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Infrastructure.Configuration;

namespace RbacSystem.Infrastructure.Services;

/// <inheritdoc cref="ITokenService" />
public sealed class JwtTokenService(
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthTokenOptions> authTokenOptions,
    IRefreshTokenRepository refreshTokenRepository,
    RefreshTokenHasher refreshTokenHasher,
    TimeProvider timeProvider) : ITokenService
{
    /// <summary>
    /// Entropy of a refresh token, in bytes, matching the sibling implementations.
    /// </summary>
    private const int refreshTokenBytes = 48;

    /// <summary>Claim carrying the user's token version, used to invalidate sessions in bulk.</summary>
    public const string TokenVersionClaim = "token_version";

    /// <summary>
    /// Claim carrying the user's role.
    /// </summary>
    /// <remarks>
    /// A short name rather than the <see cref="ClaimTypes.Role"/> schema URI, so the
    /// token reads the same as the sibling implementations. The API pairs this with
    /// <c>TokenValidationParameters.RoleClaimType</c> so role-based authorization
    /// resolves it regardless of inbound claim mapping.
    /// </remarks>
    public const string RoleClaim = "role";

    /// <inheritdoc />
    public async Task<IssuedTokens> IssueTokenPairAsync(
        User user,
        string tokenFamily,
        string? userAgent,
        IPAddress? ipAddress,
        string? rotatedFromId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenFamily);

        JwtOptions jwt = jwtOptions.Value;
        AuthTokenOptions lifetimes = authTokenOptions.Value;
        DateTime issuedAt = timeProvider.GetUtcNow().UtcDateTime;
        DateTime accessExpiresAt = issuedAt.AddMinutes(lifetimes.AccessTokenExpiryMinutes);

        string accessToken = CreateAccessToken(user, jwt, tokenFamily, issuedAt, accessExpiresAt);
        string rawRefreshToken = CreateRefreshToken();

        RefreshToken refreshToken = new()
        {
            UserId = user.Id,
            TokenHash = refreshTokenHasher.Hash(rawRefreshToken),
            TokenFamily = tokenFamily,
            RotatedFromId = rotatedFromId,
            ExpiresAt = issuedAt.AddDays(lifetimes.RefreshTokenExpiryDays),
            UserAgent = Truncate(userAgent, 500),
            IpAddress = ipAddress,
            CreatedAt = issuedAt
        };

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new IssuedTokens(
            accessToken,
            rawRefreshToken,
            lifetimes.AccessTokenExpiryMinutes * 60);
    }

    private static string CreateAccessToken(
        User user,
        JwtOptions jwt,
        string tokenFamily,
        DateTime issuedAt,
        DateTime expiresAt)
    {
        SigningCredentials credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        // Claims are supplied as a dictionary rather than a ClaimsIdentity on
        // purpose: a ClaimsIdentity is run through outbound claim-type mapping,
        // which silently rewrites "role" to the long schema URI and would break
        // parity with the sibling services. The dictionary is written verbatim.
        //
        // sid carries the token family, which is this session's identifier: every
        // refresh token rotated from this login shares it.
        Dictionary<string, object> claims = new(StringComparer.Ordinal)
        {
            [JwtRegisteredClaimNames.Sub] = user.Id,
            [JwtRegisteredClaimNames.Email] = user.Email,
            [RoleClaim] = RoleName(user.Role),
            [JwtRegisteredClaimNames.Sid] = tokenFamily,
            [JwtRegisteredClaimNames.Jti] = EntityId.New(),
            [TokenVersionClaim] = user.TokenVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        SecurityTokenDescriptor descriptor = new()
        {
            Claims = claims,
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Produces an opaque, cryptographically random refresh token.
    /// </summary>
    /// <remarks>
    /// Deliberately not a JWT: the value carries no claims, is never parsed, and is
    /// only ever compared against a stored hash, so randomness is the whole security
    /// property. Base64Url keeps it safe to place in headers and JSON.
    /// </remarks>
    private static string CreateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(refreshTokenBytes));
    }

    /// <summary>Maps the role enum to the lowercase form stored in the database.</summary>
    private static string RoleName(UserRole role)
    {
        return role == UserRole.Admin ? "admin" : "user";
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
