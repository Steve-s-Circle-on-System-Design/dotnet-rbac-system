using System.ComponentModel.DataAnnotations;

namespace RbacSystem.Infrastructure.Configuration;

/// <summary>
/// Password hashing settings bound from the <c>Security:PasswordHashing</c>
/// configuration section.
/// </summary>
/// <remarks>
/// The cost is configuration rather than a compile-time constant so it can be
/// raised as hardware improves, and lowered in test or CI environments where a
/// production-grade cost would dominate run time, without a code change.
/// </remarks>
public sealed class PasswordHashingOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Security:PasswordHashing";

    /// <summary>Cost used when no value is configured.</summary>
    public const int DefaultWorkFactor = 12;

    /// <summary>
    /// BCrypt cost factor. Each increment doubles the work required to compute a
    /// hash. BCrypt itself accepts 4 to 31.
    /// </summary>
    [Range(4, 31, ErrorMessage = "Security:PasswordHashing:WorkFactor must be between 4 and 31.")]
    public int WorkFactor { get; set; } = DefaultWorkFactor;
}
