using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Deterministic <see cref="IPasswordHasher"/> so registration tests can assert on
/// the stored value without paying BCrypt's cost.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    /// <summary>Prefix applied to hashed values.</summary>
    public const string Prefix = "fake-hash::";

    /// <summary>Passwords passed to <see cref="Hash"/>.</summary>
    public List<string> HashedPasswords { get; } = [];

    /// <inheritdoc />
    public string Hash(string password)
    {
        HashedPasswords.Add(password);

        return Prefix + password.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
