using System.ComponentModel.DataAnnotations;
using RbacSystem.Application.Features.Auth.Login;

namespace RbacSystem.Tests.Features.Auth.Login;

/// <summary>
/// Validates the data-annotation rules on <see cref="LoginRequest"/>.
/// </summary>
public class LoginRequestValidationTests
{
    private const string validEmail = "ada@example.com";

    private static List<ValidationResult> Validate(string email, string password)
    {
        LoginRequest request = new() { Email = email, Password = password };
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
    public void Request_IsValid_WithAnEmailAndPassword()
    {
        Assert.True(IsValid(validEmail, "Str0ng!Passw0rd"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@example.com")]
    public void Request_IsInvalid_ForAMalformedEmail(string email)
    {
        Assert.False(IsValid(email, "Str0ng!Passw0rd"));
    }

    [Fact]
    public void Request_IsInvalid_WhenPasswordIsMissing()
    {
        Assert.False(IsValid(validEmail, string.Empty));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("alllowercase")]
    [InlineData("NOSPECIALCHARS1")]
    [InlineData("short")]
    public void Request_AcceptsPasswordsTheRegistrationPolicyWouldReject(string weakPassword)
    {
        // Login must not apply the registration password policy. Rejecting a weak
        // password here would answer with 400 instead of 401, disclosing the policy
        // to an attacker and locking out anyone whose password predates it.
        Assert.True(IsValid(validEmail, weakPassword));
    }

    [Fact]
    public void Request_IsInvalid_WhenEmailExceedsColumnLength()
    {
        Assert.False(IsValid(new string('a', 250) + "@example.com", "Str0ng!Passw0rd"));
    }
}
