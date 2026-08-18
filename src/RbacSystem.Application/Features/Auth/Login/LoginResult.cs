namespace RbacSystem.Application.Features.Auth.Login;

/// <summary>
/// Result of a login attempt: an outcome, plus the issued tokens when it succeeded.
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Response">The token pair, present only when <paramref name="Outcome"/> is success.</param>
public sealed record LoginResult(LoginOutcome Outcome, LoginResponse? Response)
{
    /// <summary>Creates a successful result carrying the issued tokens.</summary>
    public static LoginResult Success(LoginResponse response)
    {
        return new LoginResult(LoginOutcome.Success, response);
    }

    /// <summary>Creates a failed result for the supplied outcome.</summary>
    public static LoginResult Failed(LoginOutcome outcome)
    {
        return new LoginResult(outcome, null);
    }
}
