using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;

namespace RbacSystem.Tests.Infrastructure;

/// <summary>
/// Tests the real BCrypt hasher, including that its cost comes from configuration.
/// </summary>
public partial class BCryptPasswordHasherTests
{
    private const string Password = "Str0ng!Passw0rd";

    /// <summary>
    /// Cheapest cost BCrypt accepts. Used so the suite stays fast; the production
    /// default is asserted separately without hashing.
    /// </summary>
    private const int FastWorkFactor = 4;

    private static BCryptPasswordHasher CreateHasher(int workFactor)
    {
        return new BCryptPasswordHasher(
            Options.Create(new PasswordHashingOptions { WorkFactor = workFactor }));
    }

    /// <summary>Matches a BCrypt hash prefix, capturing the encoded cost.</summary>
    [GeneratedRegex(@"^\$2[abxy]\$(\d{2})\$")]
    private static partial Regex BCryptPrefix();

    [Fact]
    public void DefaultWorkFactor_Is12()
    {
        Assert.Equal(12, PasswordHashingOptions.DefaultWorkFactor);
        Assert.Equal(12, new PasswordHashingOptions().WorkFactor);
    }

    [Fact]
    public void Hash_ProducesBCryptHash_ThatIsNotThePlaintext()
    {
        string hash = CreateHasher(FastWorkFactor).Hash(Password);

        Assert.Matches(BCryptPrefix(), hash);
        Assert.NotEqual(Password, hash);
        Assert.DoesNotContain(Password, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Hash_ProducesVerifiableHash()
    {
        string hash = CreateHasher(FastWorkFactor).Hash(Password);

        Assert.True(BCrypt.Net.BCrypt.Verify(Password, hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_UsesFreshSalt_SoIdenticalPasswordsDiffer()
    {
        BCryptPasswordHasher hasher = CreateHasher(FastWorkFactor);

        Assert.NotEqual(hasher.Hash(Password), hasher.Hash(Password));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public void Hash_AppliesConfiguredWorkFactor(int workFactor)
    {
        string hash = CreateHasher(workFactor).Hash(Password);

        Match match = BCryptPrefix().Match(hash);

        Assert.True(match.Success);
        Assert.Equal(workFactor, int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    public void Constructor_Throws_ForWorkFactorOutsideBCryptRange(int workFactor)
    {
        // A misconfigured cost must fail loudly rather than silently hash weakly.
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => CreateHasher(workFactor));
    }

    [Fact]
    public void Constructor_Throws_WhenOptionsAreNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BCryptPasswordHasher(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_Throws_ForMissingPassword(string? password)
    {
        BCryptPasswordHasher hasher = CreateHasher(FastWorkFactor);

        _ = Assert.ThrowsAny<ArgumentException>(() => hasher.Hash(password!));
    }
}
