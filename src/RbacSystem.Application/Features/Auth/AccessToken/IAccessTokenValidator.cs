namespace RbacSystem.Application.Features.Auth.AccessToken;

/// <summary>Validates an access token against the user's current security state.</summary>
public interface IAccessTokenValidator
{
    /// <summary>
    /// Determines whether the token version issued for a user is still current and
    /// whether the account remains eligible to make authenticated requests.
    /// </summary>
    Task<bool> IsValidAsync(
        string userId,
        int tokenVersion,
        CancellationToken cancellationToken = default);
}
