namespace RbacSystem.Application.Interfaces.Services;

/// <summary>
/// Produces one-way hashes of user passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password for storage.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The hash, including its salt and cost parameters.</returns>
    string Hash(string password);
}
