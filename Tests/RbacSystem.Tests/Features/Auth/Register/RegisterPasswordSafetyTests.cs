using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Tests.Features.Auth.Register;

/// <summary>
/// Guards the acceptance criterion that a plain-text password is never stored or
/// logged.
/// </summary>
public class RegisterPasswordSafetyTests
{
    private const string password = "Str0ng!Passw0rd";

    /// <summary>Mirrors the camelCase policy ASP.NET Core applies to responses.</summary>
    private static readonly JsonSerializerOptions wireFormat =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void RegisterRequest_ToString_DoesNotRevealPassword()
    {
        RegisterRequest request = new() { Email = "ada@example.com", Password = password };

        // ASP.NET Core MVC logs bound action arguments at Information level via
        // ToString(). RegisterRequest is deliberately a class, not a record: a
        // record's synthesized ToString() prints every property, which would write
        // the plain-text password straight into the application log.
        Assert.DoesNotContain(password, request.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationMessages_DoNotEchoTheSubmittedPassword()
    {
        // "weakpass" fails the uppercase and special-character rules.
        RegisterRequest request = new() { Email = "ada@example.com", Password = "weakpass" };
        List<ValidationResult> results = [];

        _ = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        ValidationResult failure = Assert.Single(results);

        Assert.DoesNotContain("weakpass", failure.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterResponse_CarriesOnlyAMessage()
    {
        // Serialized with the camelCase policy ASP.NET Core applies, so this asserts
        // the wire format the sibling implementations expect.
        string json = JsonSerializer.Serialize(
            new RegisterResponse("Sign Up successful, verify Email."),
            wireFormat);

        Assert.DoesNotContain("assword", json, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);

        JsonProperty only = Assert.Single(document.RootElement.EnumerateObject());

        Assert.Equal("message", only.Name);
        Assert.Equal("Sign Up successful, verify Email.", only.Value.GetString());
    }
}
