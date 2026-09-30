namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Produces the protected, deterministic representation used to locate a refresh
/// token without storing its raw value.
/// </summary>
public interface IRefreshTokenHasher
{
    /// <summary>
    /// Hashes a raw refresh token for persistence or database lookup.
    /// </summary>
    /// <param name="rawToken">The client-held raw refresh token.</param>
    /// <returns>The deterministic protected token hash.</returns>
    string Hash(string rawToken);
}
