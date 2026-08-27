using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Domain.Entities;
using RbacSystem.Infrastructure.Persistence;
using RbacSystem.Infrastructure.Repositories;

namespace RbacSystem.Tests.Database;

/// <summary>
/// Exercises the atomic failed-login statement against a real PostgreSQL.
/// </summary>
/// <remarks>
/// The whole point of doing this in one statement is behaviour under concurrency,
/// which no in-memory fake can demonstrate. These tests are the evidence for the
/// acceptance criteria rather than a restatement of the unit tests.
/// </remarks>
public sealed class RegisterFailedLoginTests : IClassFixture<PostgresFixture>
{
    private const int maxAttempts = 5;

    private static readonly TimeSpan lockoutDuration = TimeSpan.FromMinutes(15);

    private static async Task<FailedLoginOutcome> RegisterAsync(string userId, DateTime nowUtc)
    {
        await using AppDbContext context = PostgresFixture.CreateContext();

        return await new UserRepository(context)
            .RegisterFailedLoginAsync(userId, maxAttempts, lockoutDuration, nowUtc);
    }

    [RequiresPostgresFact]
    public async Task FailuresBelowTheThreshold_IncrementWithoutLocking()
    {
        User user = await PostgresFixture.SeedUserAsync();
        DateTime now = DateTime.UtcNow;

        for (int attempt = 1; attempt < maxAttempts; attempt++)
        {
            FailedLoginOutcome outcome = await RegisterAsync(user.Id, now);

            Assert.Equal(attempt, outcome.FailedAttempts);
            Assert.Null(outcome.LockoutEnd);
            Assert.False(outcome.LockoutJustStarted);
        }

        Assert.Null((await PostgresFixture.ReloadAsync(user.Id)).LockoutEnd);
    }

    [RequiresPostgresFact]
    public async Task TheThresholdAttempt_AppliesTheLockAndReportsTheTransition()
    {
        User user = await PostgresFixture.SeedUserAsync(failedAttempts: maxAttempts - 1);
        DateTime now = DateTime.UtcNow;

        FailedLoginOutcome outcome = await RegisterAsync(user.Id, now);

        Assert.Equal(maxAttempts, outcome.FailedAttempts);
        Assert.True(outcome.LockoutJustStarted);
        _ = Assert.NotNull(outcome.LockoutEnd);

        User stored = await PostgresFixture.ReloadAsync(user.Id);

        _ = Assert.NotNull(stored.LockoutEnd);
        Assert.InRange(
            stored.LockoutEnd.Value,
            now.Add(lockoutDuration).AddSeconds(-5),
            now.Add(lockoutDuration).AddSeconds(5));
    }

    [RequiresPostgresFact]
    public async Task AnAttemptDuringAnActiveLockout_ChangesNothing()
    {
        DateTime now = DateTime.UtcNow;
        DateTime lockedUntil = now.AddMinutes(10);
        User user = await PostgresFixture.SeedUserAsync(failedAttempts: maxAttempts, lockoutEnd: lockedUntil);

        FailedLoginOutcome outcome = await RegisterAsync(user.Id, now);

        Assert.False(outcome.LockoutJustStarted);

        User stored = await PostgresFixture.ReloadAsync(user.Id);

        // The count must not climb and, critically, the expiry must not move: an
        // attacker hammering a locked account must not be able to extend the lockout.
        Assert.Equal(maxAttempts, stored.FailedLoginAttempts);
        _ = Assert.NotNull(stored.LockoutEnd);
        Assert.InRange(stored.LockoutEnd.Value, lockedUntil.AddSeconds(-2), lockedUntil.AddSeconds(2));
    }

    [RequiresPostgresFact]
    public async Task AnExpiredLockout_StartsANewSequenceAtOne()
    {
        DateTime now = DateTime.UtcNow;
        User user = await PostgresFixture.SeedUserAsync(
            failedAttempts: maxAttempts,
            lockoutEnd: now.AddMinutes(-1));

        FailedLoginOutcome outcome = await RegisterAsync(user.Id, now);

        // Resuming from 5 would re-lock the account on the very next mistake.
        Assert.Equal(1, outcome.FailedAttempts);
        Assert.Null(outcome.LockoutEnd);
        Assert.False(outcome.LockoutJustStarted);
        Assert.Null((await PostgresFixture.ReloadAsync(user.Id)).LockoutEnd);
    }

    [RequiresPostgresFact]
    public async Task ASoftDeletedAccount_IsNotTracked()
    {
        DateTime now = DateTime.UtcNow;
        User user = await PostgresFixture.SeedUserAsync(deletedAt: now.AddDays(-1));

        FailedLoginOutcome outcome = await RegisterAsync(user.Id, now);

        Assert.False(outcome.LockoutJustStarted);
        Assert.Equal(0, (await PostgresFixture.ReloadAsync(user.Id)).FailedLoginAttempts);
    }

    [RequiresPostgresFact]
    public async Task ConcurrentFailures_CountExactlyOnceAndLockExactlyOnce()
    {
        // The headline acceptance criterion. A read-modify-write would lose
        // increments here and could report the transition to several callers at once,
        // producing duplicate lockout alerts.
        const int concurrentAttempts = 20;

        User user = await PostgresFixture.SeedUserAsync();
        DateTime now = DateTime.UtcNow;

        FailedLoginOutcome[] outcomes = await Task.WhenAll(
            Enumerable.Range(0, concurrentAttempts).Select(_ => RegisterAsync(user.Id, now)));

        User stored = await PostgresFixture.ReloadAsync(user.Id);

        // Attempts land one at a time until the threshold applies the lock; every
        // later statement then matches zero rows, so the count stops dead on the
        // threshold instead of overshooting.
        Assert.Equal(maxAttempts, stored.FailedLoginAttempts);
        _ = Assert.NotNull(stored.LockoutEnd);

        Assert.Equal(1, outcomes.Count(outcome => outcome.LockoutJustStarted));
        Assert.Equal(maxAttempts, outcomes.Count(outcome => outcome.FailedAttempts > 0));
        Assert.All(outcomes, outcome => Assert.True(outcome.FailedAttempts <= maxAttempts));

        // Each counted attempt got a distinct number: nothing was lost or duplicated.
        int[] counted = [.. outcomes.Where(o => o.FailedAttempts > 0).Select(o => o.FailedAttempts).Order()];

        Assert.Equal(Enumerable.Range(1, maxAttempts), counted);
    }

    [RequiresPostgresFact]
    public async Task ANonUtcTimestamp_IsRejectedAtTheBoundary()
    {
        // lockout_end is timestamptz; passing local time would otherwise fail deep
        // inside the provider with a message that does not name the caller's mistake.
        User user = await PostgresFixture.SeedUserAsync();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => RegisterAsync(user.Id, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local)));
    }
}
