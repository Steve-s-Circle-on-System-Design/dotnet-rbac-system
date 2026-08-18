using Microsoft.Extensions.Options;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;

namespace RbacSystem.Tests.Infrastructure;

/// <summary>
/// Tests password verification, including the dummy hash used to equalize login
/// timing for unknown accounts.
/// </summary>
public class BCryptPasswordHasherVerifyTests
{
    private const string password = "Str0ng!Passw0rd";

    /// <summary>Cheapest cost BCrypt accepts, so the suite stays fast.</summary>
    private const int fastWorkFactor = 4;

    private static BCryptPasswordHasher CreateHasher()
    {
        return new BCryptPasswordHasher(
            Options.Create(new PasswordHashingOptions { WorkFactor = fastWorkFactor }));
    }

    [Fact]
    public void Verify_AcceptsTheCorrectPassword()
    {
        BCryptPasswordHasher hasher = CreateHasher();

        Assert.True(hasher.Verify(password, hasher.Hash(password)));
    }

    [Fact]
    public void Verify_RejectsTheWrongPassword()
    {
        BCryptPasswordHasher hasher = CreateHasher();

        Assert.False(hasher.Verify("not-the-password", hasher.Hash(password)));
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        BCryptPasswordHasher hasher = CreateHasher();

        Assert.False(hasher.Verify(password.ToUpperInvariant(), hasher.Hash(password)));
    }

    [Theory]
    [InlineData("not-a-bcrypt-hash")]
    [InlineData("$2a$notanumber$xxxx")]
    [InlineData("")]
    public void Verify_ReturnsFalseForAMalformedHash_RatherThanThrowing(string malformedHash)
    {
        // A corrupt stored hash should fail that one login, not surface as a 500.
        Assert.False(CreateHasher().Verify(password, malformedHash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Verify_ReturnsFalseForAMissingPassword(string? missingPassword)
    {
        BCryptPasswordHasher hasher = CreateHasher();

        Assert.False(hasher.Verify(missingPassword!, hasher.Hash(password)));
    }

    [Fact]
    public void DummyHash_IsAWellFormedBCryptHashThatNothingVerifiesAgainst()
    {
        BCryptPasswordHasher hasher = CreateHasher();

        // If this were malformed, Verify would short-circuit and the timing cover it
        // exists to provide would silently disappear.
        Assert.Matches(@"^\$2[abxy]\$\d{2}\$", hasher.DummyHash);
        Assert.False(hasher.Verify(password, hasher.DummyHash));
        Assert.False(hasher.Verify(string.Empty, hasher.DummyHash));
    }

    [Fact]
    public void DummyHash_IsStableForTheLifetimeOfTheHasher()
    {
        // Recomputing per call would add a full BCrypt hash to every login attempt.
        BCryptPasswordHasher hasher = CreateHasher();

        Assert.Equal(hasher.DummyHash, hasher.DummyHash);
    }

    [Fact]
    public void DummyHash_DiffersBetweenInstances()
    {
        Assert.NotEqual(CreateHasher().DummyHash, CreateHasher().DummyHash);
    }
}
