using RbacSystem.Domain.Entities;

namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Token values prepared for the client together with the refresh-token record
/// that the calling use case must persist.
/// </summary>
/// <remarks>
/// This is deliberately a class rather than a record so its generated string
/// representation cannot accidentally include the raw refresh token.
/// </remarks>
public sealed class PreparedTokenPair
{
    public PreparedTokenPair(IssuedTokens tokens, RefreshToken refreshTokenRecord)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(refreshTokenRecord);

        Tokens = tokens;
        RefreshTokenRecord = refreshTokenRecord;
    }

    /// <summary>Values that may be returned to the authenticated client.</summary>
    public IssuedTokens Tokens { get; }

    /// <summary>The hashed refresh-token record to be persisted by the use case.</summary>
    public RefreshToken RefreshTokenRecord { get; }
}
