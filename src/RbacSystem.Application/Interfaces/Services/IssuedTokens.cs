namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// An access and refresh token pair produced by <see cref="ITokenService"/>.
/// </summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="RefreshToken">
/// The raw refresh token. Only its hash is persisted, so this value exists solely to
/// be returned to the caller and must never be logged or stored.
/// </param>
/// <param name="AccessTokenExpiresInSeconds">Lifetime of the access token, in seconds.</param>
public sealed record IssuedTokens(
    string AccessToken,
    string RefreshToken,
    int AccessTokenExpiresInSeconds);
