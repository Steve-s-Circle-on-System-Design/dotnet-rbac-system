namespace RbacSystem.Application.Features.Auth.Refresh;

/// <summary>
/// Outcome of a refresh attempt that passed input validation.
/// </summary>
public enum RefreshOutcome
{
    /// <summary>The submitted token was accepted and rotated.</summary>
    Success = 0,

    /// <summary>
    /// The submitted token or its account cannot be used to refresh the session.
    /// </summary>
    /// <remarks>
    /// Unknown, expired, used and revoked tokens, and ineligible accounts, share
    /// one outcome so the public API does not reveal session or account state.
    /// </remarks>
    InvalidToken = 1
}
