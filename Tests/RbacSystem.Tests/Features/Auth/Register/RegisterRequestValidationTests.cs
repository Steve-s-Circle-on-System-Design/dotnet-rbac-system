using System.ComponentModel.DataAnnotations;
using System.Text;
using RbacSystem.Application.Common.Validation;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Tests.Features.Auth.Register;

/// <summary>
/// Validates the data-annotation rules on <see cref="RegisterRequest"/>, which are
/// what <c>[ApiController]</c> enforces before the service is reached.
/// </summary>
public class RegisterRequestValidationTests
{
    private const string validEmail = "ada@example.com";
    private const string validPassword = "Str0ng!Passw0rd";

    private static List<ValidationResult> Validate(string email, string password)
    {
        RegisterRequest request = new() { Email = email, Password = password };
        List<ValidationResult> results = [];

        _ = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        return results;
    }

    private static bool IsValid(string email, string password)
    {
        return Validate(email, password).Count == 0;
    }

    [Fact]
    public void Request_IsValid_WithConformingEmailAndPassword()
    {
        Assert.True(IsValid(validEmail, validPassword));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@example.com")]
    public void Request_IsInvalid_ForMalformedEmail(string email)
    {
        Assert.False(IsValid(email, validPassword));
    }

    [Fact]
    public void Request_IsInvalid_WhenEmailExceedsColumnLength()
    {
        string longEmail = new string('a', 250) + "@example.com";

        Assert.False(IsValid(longEmail, validPassword));
    }

    [Fact]
    public void Password_IsInvalid_WhenMissing()
    {
        Assert.False(IsValid(validEmail, string.Empty));
    }

    [Theory]
    [InlineData("Ab!c123")]      // 7 characters, one short of the minimum
    [InlineData("Ab!c1")]
    public void Password_IsInvalid_WhenShorterThanMinimumLength(string password)
    {
        Assert.Contains(
            "at least",
            Assert.Single(Validate(validEmail, password)).ErrorMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Password_IsInvalid_WithoutLowercaseLetter()
    {
        Assert.Contains(
            "lowercase",
            Assert.Single(Validate(validEmail, "PASSW0RD!")).ErrorMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Password_IsInvalid_WithoutUppercaseLetter()
    {
        Assert.Contains(
            "uppercase",
            Assert.Single(Validate(validEmail, "passw0rd!")).ErrorMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Password_IsInvalid_WithoutSpecialCharacter()
    {
        Assert.Contains(
            "special",
            Assert.Single(Validate(validEmail, "Passw0rdAb")).ErrorMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Password_ReportsEveryMissingCharacterClass()
    {
        string? message = Assert.Single(Validate(validEmail, "abcdefgh")).ErrorMessage;

        Assert.Contains("uppercase", message, StringComparison.Ordinal);
        Assert.Contains("special", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Password_TreatsWhitespaceAsNotSpecial()
    {
        // A space must not satisfy the special-character rule on its own.
        Assert.False(IsValid(validEmail, "Passw0rd Ab"));
    }

    [Fact]
    public void Password_AcceptsExactlyMaximumByteLength()
    {
        string atLimit = "Aa!" + new string('x', PasswordPolicyAttribute.MaximumByteLength - 3);

        Assert.Equal(PasswordPolicyAttribute.MaximumByteLength, Encoding.UTF8.GetByteCount(atLimit));
        Assert.True(IsValid(validEmail, atLimit));
    }

    [Fact]
    public void Password_IsInvalid_WhenExceedingMaximumByteLength()
    {
        string tooLong = "Aa!" + new string('x', PasswordPolicyAttribute.MaximumByteLength - 2);

        Assert.Contains(
            "bytes",
            Assert.Single(Validate(validEmail, tooLong)).ErrorMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Password_MeasuresLimitInBytes_NotCharacters()
    {
        // 30 four-byte characters are 120 UTF-8 bytes but only 30 chars, so a naive
        // character count would wrongly accept a password BCrypt would truncate.
        string multiByte = "Aa!" + string.Concat(Enumerable.Repeat("😀", 30));

        Assert.True(multiByte.Length <= PasswordPolicyAttribute.MaximumByteLength);
        Assert.True(Encoding.UTF8.GetByteCount(multiByte) > PasswordPolicyAttribute.MaximumByteLength);
        Assert.False(IsValid(validEmail, multiByte));
    }
}
