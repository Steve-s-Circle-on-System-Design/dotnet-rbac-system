using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Infrastructure.Configuration;

/// <summary>
/// JWT signing and validation settings bound from the <c>Jwt</c> configuration section.
/// </summary>
/// <remarks>
/// <see cref="Key"/> and <see cref="RefreshTokenHashSecret"/> are secrets and must
/// come from user secrets locally, or environment variables or a managed secret
/// store in production. Only the issuer and audience belong in appsettings.json.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Shortest signing key accepted, in bytes. HMAC-SHA256 requires a key at least
    /// as long as its 256-bit output; anything shorter weakens the signature and is
    /// rejected outright by the token handler.
    /// </summary>
    public const int MinimumKeyBytes = 32;

    /// <summary>Token issuer, validated on every incoming token.</summary>
    [Required(ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Intended audience, validated on every incoming token.</summary>
    [Required(ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key for access tokens.</summary>
    /// <remarks>
    /// The length floor is enforced rather than advisory: HMAC-SHA256 rejects a key
    /// shorter than its 256-bit output, so without this a short key would surface as
    /// an obscure failure on the first login instead of a clear error at startup.
    /// </remarks>
    [Required(ErrorMessage = "Jwt:Key is required. Set it with dotnet user-secrets.")]
    [MinLength(MinimumKeyBytes, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Secret used to HMAC refresh tokens before storage.
    /// </summary>
    /// <remarks>
    /// Keyed hashing rather than a plain digest means that a leaked database alone
    /// does not let an attacker match stolen refresh tokens to stored rows.
    /// </remarks>
    [Required(ErrorMessage = "Jwt:RefreshTokenHashSecret is required. Set it with dotnet user-secrets.")]
    [MinLength(MinimumKeyBytes, ErrorMessage = "Jwt:RefreshTokenHashSecret must be at least 32 characters.")]
    public string RefreshTokenHashSecret { get; set; } = string.Empty;
}
