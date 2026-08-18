namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Produces and checks one-way hashes of user passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password for storage.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The hash, including its salt and cost parameters.</returns>
    string Hash(string password);

    /// <summary>
    /// Checks a plain-text password against a stored hash.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> rather than throwing when the stored hash is
    /// malformed, so a corrupt record fails the login instead of the request.
    /// </remarks>
    /// <param name="password">The plain-text password supplied by the caller.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    bool Verify(string password, string hash);

    /// <summary>
    /// A valid hash of an unguessable value, produced at the configured cost.
    /// </summary>
    /// <remarks>
    /// Verifying against this when no account exists makes a failed lookup cost the
    /// same as a real password check, so response time cannot be used to enumerate
    /// registered addresses. It is generated rather than hardcoded so it is always a
    /// well-formed hash at the current cost — a malformed constant would be rejected
    /// instantly and provide no timing cover at all.
    /// </remarks>
    string DummyHash { get; }
}
