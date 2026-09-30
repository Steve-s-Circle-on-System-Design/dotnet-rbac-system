using System.Net;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Common;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Records the arguments login passes to token issuance, so the login tests can
/// assert on session handling without signing real JWTs.
/// </summary>
internal sealed class FakeTokenService : ITokenService
{
    /// <summary>Every issuance request, in order.</summary>
    public List<(User User, string TokenFamily, string? UserAgent, IPAddress? IpAddress, string? RotatedFromId)> Issued { get; } = [];

    /// <summary>Tokens handed back to the caller.</summary>
    public IssuedTokens Result { get; set; } = new("access-token", "refresh-token", 900);

    /// <inheritdoc />
    public PreparedTokenPair IssueTokenPair(
        User user,
        string tokenFamily,
        string? userAgent,
        IPAddress? ipAddress,
        string? rotatedFromId = null)
    {
        Issued.Add((user, tokenFamily, userAgent, ipAddress, rotatedFromId));

        return new PreparedTokenPair(
            Result,
            new RefreshToken
            {
                Id = EntityId.New(),
                UserId = user.Id,
                TokenHash = "prepared-refresh-token-hash",
                TokenFamily = tokenFamily,
                RotatedFromId = rotatedFromId,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
    }
}
