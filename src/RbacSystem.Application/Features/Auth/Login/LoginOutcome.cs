namespace RbacSystem.Application.Features.Auth.Login;

/// <summary>
/// Outcome of a login attempt that passed input validation.
/// </summary>
public enum LoginOutcome
{
    /// <summary>Credentials accepted and a token pair was issued.</summary>
    Success = 0,

    /// <summary>
    /// The email is not registered, or the password does not match.
    /// </summary>
    /// <remarks>
    /// The two cases share one value on purpose: distinguishing them would let an
    /// unauthenticated caller enumerate registered addresses.
    /// </remarks>
    InvalidCredentials = 1,

    /// <summary>The account exists but its email address has not been verified.</summary>
    EmailNotVerified = 2,

    /// <summary>The account is temporarily locked after repeated failed attempts.</summary>
    AccountLocked = 3
}
