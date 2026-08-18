using System.Net;
using RbacSystem.Application.Interfaces.Services;
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
    public Task<IssuedTokens> IssueTokenPairAsync(
        User user,
        string tokenFamily,
        string? userAgent,
        IPAddress? ipAddress,
        string? rotatedFromId = null,
        CancellationToken cancellationToken = default)
    {
        Issued.Add((user, tokenFamily, userAgent, ipAddress, rotatedFromId));

        return Task.FromResult(Result);
    }
}
