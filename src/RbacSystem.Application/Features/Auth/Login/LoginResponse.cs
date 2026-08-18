namespace RbacSystem.Application.Features.Auth.Login;

/// <summary>
/// Successful login response.
/// </summary>
/// <param name="AccessToken">Signed JWT used to authenticate subsequent requests.</param>
/// <param name="RefreshToken">Opaque token used to obtain a new pair once the access token expires.</param>
/// <param name="TokenType">Authorization scheme the access token is presented with.</param>
/// <param name="ExpiresIn">Access-token lifetime in seconds, so clients need not hardcode it.</param>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);
