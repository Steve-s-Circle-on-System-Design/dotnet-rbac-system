using Microsoft.Extensions.Options;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Infrastructure.Configuration;

namespace RbacSystem.Infrastructure.Services;

/// <inheritdoc cref="IPasswordHasher" />
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private readonly int workFactor;

    /// <summary>
    /// Initializes the hasher from the configured <see cref="PasswordHashingOptions"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the configured cost is outside the range BCrypt accepts, so that a
    /// misconfigured deployment fails immediately instead of hashing weakly.
    /// </exception>
    public BCryptPasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        int configuredWorkFactor = options.Value.WorkFactor;

        if (configuredWorkFactor is < 4 or > 31)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                configuredWorkFactor,
                $"{PasswordHashingOptions.SectionName}:WorkFactor must be between 4 and 31.");
        }

        workFactor = configuredWorkFactor;
    }

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        // EnhancedHashPassword is avoided on purpose: it pre-hashes with SHA-384,
        // which the sibling implementations do not, so a hash produced here would
        // not verify against them.
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    }
}
