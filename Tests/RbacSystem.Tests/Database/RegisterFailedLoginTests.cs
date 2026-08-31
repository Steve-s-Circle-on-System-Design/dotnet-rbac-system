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

    private static async Task<bool> CompleteAsync(string userId, DateTime nowUtc)
    {
        await using AppDbContext context = PostgresFixture.CreateContext();

        return await new UserRepository(context)
            .TryCompleteSuccessfulLoginAsync(userId, nowUtc, null);
    }

    [RequiresPostgresFact]
    public async Task ASuccessfulLogin_ClearsTheFailureStateWhenNotLocked()
    {
        User user = await PostgresFixture.SeedUserAsync(failedAttempts: 3);
        DateTime now = DateTime.UtcNow;

        Assert.True(await CompleteAsync(user.Id, now));

        User stored = await PostgresFixture.ReloadAsync(user.Id);

        Assert.Equal(0, stored.FailedLoginAttempts);
        Assert.Null(stored.LockoutEnd);
        _ = Assert.NotNull(stored.LastLoginAt);
    }

    [RequiresPostgresFact]
    public async Task ASuccessfulLogin_IsRefusedWhileLocked_AndLeavesTheLockIntact()
    {
        DateTime now = DateTime.UtcNow;
        DateTime lockedUntil = now.AddMinutes(10);
        User user = await PostgresFixture.SeedUserAsync(failedAttempts: maxAttempts, lockoutEnd: lockedUntil);

        Assert.False(await CompleteAsync(user.Id, now));

        User stored = await PostgresFixture.ReloadAsync(user.Id);

        // The whole point: a correct password arriving during a lockout must not
        // clear it. Writing the reset through the change tracker would have.
        _ = Assert.NotNull(stored.LockoutEnd);
        Assert.InRange(stored.LockoutEnd.Value, lockedUntil.AddSeconds(-2), lockedUntil.AddSeconds(2));
        Assert.Equal(maxAttempts, stored.FailedLoginAttempts);
    }

    [RequiresPostgresFact]
    public async Task ASuccessfulLogin_IsAllowedOnceTheLockoutHasExpired()
    {
        DateTime now = DateTime.UtcNow;
        User user = await PostgresFixture.SeedUserAsync(
            failedAttempts: maxAttempts,
            lockoutEnd: now.AddMinutes(-1));

        Assert.True(await CompleteAsync(user.Id, now));
        Assert.Null((await PostgresFixture.ReloadAsync(user.Id)).LockoutEnd);
    }

    [RequiresPostgresFact]
    public async Task CorrectAndIncorrectLogins_RunningConcurrently_NeverCancelALockout()
    {
        // Requested in review. One correct-password completion races a burst of wrong
        // passwords. Whatever the interleaving, the end state must be coherent: either
        // the reset won and there is no lock, or the lock won and the reset was
        // refused. A lock must never be left cleared by a stale successful request.
        const int rounds = 25;

        for (int round = 0; round < rounds; round++)
        {
            User user = await PostgresFixture.SeedUserAsync(failedAttempts: maxAttempts - 1);
            DateTime now = DateTime.UtcNow;

            // The results are captured into plainly typed locals rather than held in
            // Task-typed ones: whether Task.Run makes its generic argument "apparent"
            // is judged differently by different SDK patch versions, so one analyzer
            // demands var there and another demands the explicit type.
            bool resetApplied = false;
            FailedLoginOutcome? failureOutcome = null;

            await Task.WhenAll(
                Task.Run(async () => { resetApplied = await CompleteAsync(user.Id, now); }),
                Task.Run(async () => { failureOutcome = await RegisterAsync(user.Id, now); }));

            Assert.NotNull(failureOutcome);

            User stored = await PostgresFixture.ReloadAsync(user.Id);

            if (failureOutcome.LockoutJustStarted && !resetApplied)
            {
                // The failure locked the account and the reset was correctly refused.
                _ = Assert.NotNull(stored.LockoutEnd);
                Assert.Equal(maxAttempts, stored.FailedLoginAttempts);
                continue;
            }

            if (resetApplied && !failureOutcome.LockoutJustStarted)
            {
                // The reset landed first, so the later failure counted from zero and
                // could not reach the threshold on its own.
                Assert.True(stored.FailedLoginAttempts <= 1);
                Assert.Null(stored.LockoutEnd);
                continue;
            }

            // Both succeeding would mean a lock was applied and then silently
            // cancelled by the stale successful request, which is the bug under test.
            Assert.Fail(
                $"Round {round}: reset={resetApplied}, lockStarted={failureOutcome.LockoutJustStarted}, " +
                $"stored attempts={stored.FailedLoginAttempts}, lockoutEnd={stored.LockoutEnd:o}");
        }
    }
}
