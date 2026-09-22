namespace RbacSystem.Application.Features.Auth.Refresh;

/// <summary>
/// Result of a refresh attempt, including the new token pair only on success.
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Response">The rotated token pair, present only on success.</param>
public sealed record RefreshResult(RefreshOutcome Outcome, RefreshResponse? Response)
{
    /// <summary>Creates a successful result carrying the rotated token pair.</summary>
    public static RefreshResult Success(RefreshResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new RefreshResult(RefreshOutcome.Success, response);
    }

    /// <summary>Creates a failed result without exposing the rejection reason.</summary>
    public static RefreshResult Invalid()
    {
        return new RefreshResult(RefreshOutcome.InvalidToken, null);
    }
}
