using System.Net;
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

    /// <summary>Users returned by <see cref="GetByEmailAsync"/>, keyed by email.</summary>
    private readonly Dictionary<string, User> usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Number of times <see cref="SaveChangesAsync"/> was called.</summary>
    public int SaveChangesCallCount { get; private set; }

    /// <summary>Email addresses seen by <see cref="GetByEmailAsync"/>.</summary>
    public List<string> GetByEmailArguments { get; } = [];

    /// <summary>Seeds an already-registered address.</summary>
    public void SeedExistingEmail(string email)
    {
        _ = existingEmails.Add(email);
    }

    /// <summary>Seeds a full user record retrievable by email.</summary>
    public void SeedUser(User user)
    {
        _ = existingEmails.Add(user.Email);
        usersByEmail[user.Email] = user;
        usersById[user.Id] = user;
    }


    /// <summary>Arguments passed to <see cref="RegisterFailedLoginAsync"/>, in order.</summary>
    public List<(string UserId, int MaxAttempts, TimeSpan Duration, DateTime NowUtc)> FailedLoginCalls { get; } = [];

    /// <summary>Users keyed by id, so failed-login bookkeeping can mutate them.</summary>
    private readonly Dictionary<string, User> usersById = new(StringComparer.Ordinal);

    /// <inheritdoc />
    /// <remarks>
    /// Mirrors the real statement's semantics rather than just counting calls: an
    /// active lockout records nothing, an expired one restarts at 1, and the lock is
    /// reported as newly started only on the transition. A fake that merely
    /// incremented would let the service pass tests the database would fail.
    /// </remarks>
    public Task<FailedLoginOutcome> RegisterFailedLoginAsync(
        string userId,
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        FailedLoginCalls.Add((userId, maxFailedAttempts, lockoutDuration, nowUtc));

        if (!usersById.TryGetValue(userId, out User? user))
        {
            return Task.FromResult(FailedLoginOutcome.AlreadyLocked(null));
        }

        if (user.LockoutEnd is { } active && active > nowUtc)
        {
            return Task.FromResult(FailedLoginOutcome.AlreadyLocked(active));
        }

        bool expired = user.LockoutEnd is not null;

        user.FailedLoginAttempts = expired ? 1 : user.FailedLoginAttempts + 1;
        user.LockoutEnd = user.FailedLoginAttempts >= maxFailedAttempts
            ? nowUtc.Add(lockoutDuration)
            : null;

        bool justStarted = user.LockoutEnd is { } end && end > nowUtc;

        return Task.FromResult(new FailedLoginOutcome(user.FailedLoginAttempts, user.LockoutEnd, justStarted));
    }


    /// <summary>
    /// Runs at the start of <see cref="TryCompleteSuccessfulLoginAsync"/>, so a test
    /// can simulate a concurrent failed attempt landing between the user being read
    /// and the successful sign-in being committed.
    /// </summary>
    public Action? OnCompleteSuccessfulLogin { get; set; }

    /// <summary>Number of times <see cref="TryCompleteSuccessfulLoginAsync"/> was called.</summary>
    public int CompleteSuccessfulLoginCallCount { get; private set; }

    /// <inheritdoc />
    /// <remarks>
    /// Mirrors the real statement: the reset is refused outright when a lockout is
    /// active, rather than blindly clearing it.
    /// </remarks>
    public Task<bool> TryCompleteSuccessfulLoginAsync(
        string userId,
        DateTime nowUtc,
        IPAddress? ipAddress,
        CancellationToken cancellationToken = default)
    {
        CompleteSuccessfulLoginCallCount++;
        OnCompleteSuccessfulLogin?.Invoke();

        if (!usersById.TryGetValue(userId, out User? user))
        {
            return Task.FromResult(false);
        }

        if (user.LockoutEnd is { } active && active > nowUtc)
        {
            return Task.FromResult(false);
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = nowUtc;
        user.LastLoginIp = ipAddress;

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        GetByEmailArguments.Add(email);

        return Task.FromResult(usersByEmail.TryGetValue(email, out User? user) ? user : null);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        return Task.CompletedTask;
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
