using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RbacSystem.Application.Common.Validation;

/// <summary>
/// Validates a plain-text password against the agreed password policy.
/// </summary>
/// <remarks>
/// Character classes are checked explicitly rather than with a lookahead regular
/// expression: the failure message can then name the specific rule that failed, and
/// there is no catastrophic-backtracking surface on attacker-supplied input.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PasswordPolicyAttribute : ValidationAttribute
{
    /// <summary>Fewest characters a password may contain.</summary>
    public const int MinimumLength = 8;

    /// <summary>
    /// Largest password accepted, in UTF-8 bytes.
    /// </summary>
    /// <remarks>
    /// BCrypt silently ignores input beyond 72 bytes, so anything longer is rejected
    /// rather than truncated: two different passwords sharing a 72-byte prefix must
    /// never authenticate the same account. The limit is measured in bytes, not
    /// characters, because non-ASCII characters occupy several bytes each.
    /// </remarks>
    public const int MaximumByteLength = 72;

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // A missing password is reported by [Required]; reporting it here as well
        // would surface the same problem twice.
        if (value is not string password || password.Length == 0)
        {
            return ValidationResult.Success;
        }

        string[] memberNames = validationContext.MemberName is null
            ? []
            : [validationContext.MemberName];

        if (password.Length < MinimumLength)
        {
            return new ValidationResult(
                $"Password must be at least {MinimumLength} characters long.",
                memberNames);
        }

        if (Encoding.UTF8.GetByteCount(password) > MaximumByteLength)
        {
            return new ValidationResult(
                $"Password must not exceed {MaximumByteLength} bytes.",
                memberNames);
        }

        bool hasLower = false;
        bool hasUpper = false;
        bool hasSpecial = false;

        foreach (char character in password)
        {
            if (char.IsLower(character))
            {
                hasLower = true;
            }
            else if (char.IsUpper(character))
            {
                hasUpper = true;
            }
            else if (!char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character))
            {
                hasSpecial = true;
            }
        }

        List<string> missing = [];

        if (!hasLower)
        {
            missing.Add("a lowercase letter");
        }

        if (!hasUpper)
        {
            missing.Add("an uppercase letter");
        }

        if (!hasSpecial)
        {
            missing.Add("a special character");
        }

        return missing.Count == 0
            ? ValidationResult.Success
            : new ValidationResult($"Password must contain {string.Join(", ", missing)}.", memberNames);
    }
}
