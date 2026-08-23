using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace RbacSystem.Tests.Integration;

/// <summary>
/// Hosts the real API in memory so the authentication pipeline can be tested as it
/// actually runs, rather than as the token-issuing code assumes it runs.
/// </summary>
internal sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Signing key used by both the factory and the tests.</summary>
    public const string SigningKey = "integration-test-signing-key-at-least-32-chars";

    /// <summary>Issuer the hosted API validates against.</summary>
    public const string Issuer = "RbacSystemTests";

    /// <summary>Audience the hosted API validates against.</summary>
    public const string Audience = "RbacSystemTestUsers";

    /// <summary>Refresh-token hash secret; unused by these tests but required at startup.</summary>
    public const string RefreshHashSecret = "integration-test-refresh-hash-secret-32-chars";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.UseEnvironment("Development");

        // UseSetting rather than ConfigureAppConfiguration: the API reads its
        // connection string and JWT settings while the host is being built, which
        // happens before an added configuration source would be visible.
        _ = builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=1;Database=integration_tests_unused;Username=none;Password=none");
        _ = builder.UseSetting("Jwt:Issuer", Issuer);
        _ = builder.UseSetting("Jwt:Audience", Audience);
        _ = builder.UseSetting("Jwt:Key", SigningKey);
        _ = builder.UseSetting("Jwt:RefreshTokenHashSecret", RefreshHashSecret);

        _ = builder.ConfigureServices(services =>
        {
            // Makes the test-only ProbeController discoverable by MapControllers
            // without adding any endpoint to the API project itself.
            _ = services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
        });
    }
}
