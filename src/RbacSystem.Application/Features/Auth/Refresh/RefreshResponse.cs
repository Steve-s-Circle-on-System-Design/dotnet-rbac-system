namespace RbacSystem.Application.Features.Auth.Refresh;

/// <summary>
/// Successful refresh response containing the rotated token pair.
/// </summary>
/// <param name="AccessToken">New signed JWT used to authenticate protected requests.</param>
/// <param name="RefreshToken">
/// New raw refresh token that replaces the submitted token. Only its HMAC hash is persisted.
/// </param>
/// <param name="TokenType">Authorization scheme used to present the access token.</param>
/// <param name="ExpiresIn">Access-token lifetime in seconds.</param>
public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);
