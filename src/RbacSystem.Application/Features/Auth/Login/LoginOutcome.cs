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
    AccountLocked = 3,

    /// <summary>
    /// The account exists and the password was correct, but the account is
    /// deactivated or suspended.
    /// </summary>
    /// <remarks>
    /// Deliberately does not distinguish between the two, so the response says an
    /// account is unusable without disclosing why.
    /// </remarks>
    AccountNotActive = 4
}
