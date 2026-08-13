using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IUserRepository"/> used to drive the registration service
/// without a database. Hand-written rather than generated so the test project does
/// not take on a mocking dependency the team has not agreed to.
/// </summary>
internal sealed class FakeUserRepository : IUserRepository
{
    private readonly HashSet<string> existingEmails = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Users accepted by <see cref="TryAddAsync"/>.</summary>
    public List<User> AddedUsers { get; } = [];

    /// <summary>Number of times <see cref="EmailExistsAsync"/> was called.</summary>
    public int EmailExistsCallCount { get; private set; }

    /// <summary>Number of times <see cref="TryAddAsync"/> was called.</summary>
    public int TryAddCallCount { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the next insert is rejected as though a
    /// concurrent request had won the unique-index race.
    /// </summary>
    public bool RejectNextAdd { get; set; }

    /// <summary>Email addresses seen by <see cref="EmailExistsAsync"/>.</summary>
    public List<string> EmailExistsArguments { get; } = [];

    /// <summary>Seeds an already-registered address.</summary>
    public void SeedExistingEmail(string email)
    {
        _ = existingEmails.Add(email);
    }

    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        EmailExistsCallCount++;
        EmailExistsArguments.Add(email);

        return Task.FromResult(existingEmails.Contains(email));
    }

    /// <inheritdoc />
    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default)
    {
        TryAddCallCount++;

        if (RejectNextAdd)
        {
            RejectNextAdd = false;
            return Task.FromResult(false);
        }

        AddedUsers.Add(user);
        _ = existingEmails.Add(user.Email);

        return Task.FromResult(true);
    }
}
