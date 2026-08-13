using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Application.Features.Auth.Register;

/// <inheritdoc cref="IRegisterUserService" />
public sealed class RegisterUserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUserRegisteredEventPublisher eventPublisher) : IRegisterUserService
{
    /// <summary>Matches the length of the <c>users.name</c> column.</summary>
    private const int MaximumNameLength = 255;

    /// <inheritdoc />
    public async Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email = NormalizeEmail(request.Email);

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return RegisterResult.DuplicateEmail;
        }

        // Role and Status are deliberately left at their entity defaults of
        // UserRole.User and UserStatus.PendingVerification: a new account starts
        // unprivileged and unverified.
        User user = new()
        {
            Email = email,
            Name = DeriveName(email),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        // A concurrent registration for the same address can win the race between
        // the check above and this insert; the unique index is the real arbiter.
        if (!await userRepository.TryAddAsync(user, cancellationToken))
        {
            return RegisterResult.DuplicateEmail;
        }

        await eventPublisher.PublishAsync(
            new UserRegisteredEvent(user.Id, user.Email, DateTime.UtcNow),
            cancellationToken);

        return RegisterResult.Success;
    }

    /// <summary>
    /// Trims and lowercases an email so stored values are canonical.
    /// </summary>
    /// <remarks>
    /// Uses invariant casing so that culture-specific rules — notably the Turkish
    /// dotless i — cannot map two different addresses onto the same value.
    /// </remarks>
    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Derives a display name from the email's local part, matching the sibling
    /// implementations, because the registration contract carries no name field.
    /// </summary>
    private static string DeriveName(string normalizedEmail)
    {
        int separatorIndex = normalizedEmail.IndexOf('@');
        string localPart = separatorIndex > 0 ? normalizedEmail[..separatorIndex] : normalizedEmail;

        return localPart.Length > MaximumNameLength
            ? localPart[..MaximumNameLength]
            : localPart;
    }
}
