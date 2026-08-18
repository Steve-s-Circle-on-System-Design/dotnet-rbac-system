using System.ComponentModel.DataAnnotations;
using RbacSystem.Application.Features.Auth.Login;

namespace RbacSystem.Tests.Features.Auth.Login;

/// <summary>
/// Guards the acceptance criterion that a plain-text password is never stored or
/// logged, for the login path specifically.
/// </summary>
public class LoginPasswordSafetyTests
{
    private const string password = "Str0ng!Passw0rd";

    [Fact]
    public void LoginRequest_ToString_DoesNotRevealPassword()
    {
        LoginRequest request = new() { Email = "ada@example.com", Password = password };

        // ASP.NET Core MVC logs bound action arguments at Information level via
        // ToString(). LoginRequest is deliberately a class, not a record: a record's
        // synthesized ToString() prints every property, which would write the
        // plain-text password straight into the application log on every attempt.
        Assert.DoesNotContain(password, request.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationMessages_DoNotEchoTheSubmittedPassword()
    {
        LoginRequest request = new() { Email = "not-an-email", Password = password };
        List<ValidationResult> results = [];

        _ = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.All(
            results,
            failure => Assert.DoesNotContain(password, failure.ErrorMessage ?? string.Empty, StringComparison.Ordinal));
    }
}
