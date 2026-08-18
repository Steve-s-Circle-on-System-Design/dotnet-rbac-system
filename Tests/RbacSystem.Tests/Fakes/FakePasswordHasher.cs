using RbacSystem.Application.Interfaces.Services;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Deterministic <see cref="IPasswordHasher"/> so registration and login tests can
/// assert on behaviour without paying BCrypt's cost.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    /// <summary>Prefix applied to hashed values.</summary>
    public const string Prefix = "fake-hash::";

    /// <summary>Value returned by <see cref="DummyHash"/>.</summary>
    public const string DummyHashValue = "fake-hash::dummy";

    /// <summary>Passwords passed to <see cref="Hash"/>.</summary>
    public List<string> HashedPasswords { get; } = [];

    /// <summary>Password and hash pairs passed to <see cref="Verify"/>, in order.</summary>
    public List<(string Password, string Hash)> VerifiedPairs { get; } = [];

    /// <summary>Result <see cref="Verify"/> returns for a non-dummy hash.</summary>
    public bool VerifyResult { get; set; } = true;

    /// <inheritdoc />
    public string DummyHash => DummyHashValue;

    /// <inheritdoc />
    public string Hash(string password)
    {
        HashedPasswords.Add(password);

        return Prefix + password.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public bool Verify(string password, string hash)
    {
        VerifiedPairs.Add((password, hash));

        // Verifying against the dummy hash must always fail, exactly as the real
        // hasher does, otherwise a test could "log in" as a non-existent user.
        return hash != DummyHashValue && VerifyResult;
    }
}
