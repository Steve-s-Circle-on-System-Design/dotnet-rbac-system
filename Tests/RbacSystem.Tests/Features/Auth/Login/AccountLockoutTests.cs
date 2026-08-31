using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RbacSystem.Application.Common.Configuration;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Features.Auth.Login;

/// <summary>
/// Failed-attempt tracking and automatic lockout, driven through the login service.
/// </summary>
public class AccountLockoutTests
{
    private const string knownEmail = "ada@example.com";
    private const string correctPassword = "Str0ng!Passw0rd";
    private const int maxAttempts = 5;
    private const int lockoutMinutes = 15;

    private static readonly DateTime now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRepository userRepository = new();
    private readonly FakePasswordHasher passwordHasher = new();
    private readonly FakeTokenService tokenService = new();
    private readonly RecordingAccountLockedEventPublisher lockedEvents = new();
    private readonly FakeTimeProvider timeProvider = new(now);

    private LoginService CreateService()
    {
        return new LoginService(
            userRepository,
            passwordHasher,
            tokenService,
            lockedEvents,
            Options.Create(new AccountLockoutOptions
            {
                MaxFailedAttempts = maxAttempts,
                DurationMinutes = lockoutMinutes
            }),
            timeProvider);
    }

    private User SeedUser()
    {
        User user = new()
        {
            Email = knownEmail,
            Name = "ada",
            PasswordHash = "$2a$12$storedhashvalue",
            Role = UserRole.User,
            Status = UserStatus.Active,
            EmailVerifiedAt = now.AddDays(-1)
        };

        userRepository.SeedUser(user);

        return user;
    }

    private static LoginRequest Request(string password)
    {
        return new LoginRequest { Email = knownEmail, Password = password };
    }

    private async Task<LoginResult> AttemptAsync(bool correct)
    {
        passwordHasher.VerifyResult = correct;

        return await CreateService().LoginAsync(Request(correct ? correctPassword : "WrongPassword!1"));
    }

    private async Task FailTimesAsync(int count)
    {
        for (int attempt = 0; attempt < count; attempt++)
        {
            _ = await AttemptAsync(correct: false);
        }
    }

    [Fact]
    public async Task AWrongPassword_IncrementsTheFailureCount()
    {
        User user = SeedUser();

        _ = await AttemptAsync(correct: false);

        Assert.Equal(1, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public async Task FailuresBelowTheThreshold_DoNotLock()
    {
        User user = SeedUser();

        await FailTimesAsync(maxAttempts - 1);

        Assert.Equal(maxAttempts - 1, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
        Assert.Empty(lockedEvents.PublishedEvents);
    }

    [Fact]
    public async Task TheThresholdAttempt_LocksForTheConfiguredWindow()
    {
        User user = SeedUser();

        await FailTimesAsync(maxAttempts);

        Assert.Equal(maxAttempts, user.FailedLoginAttempts);
        Assert.Equal(now.AddMinutes(lockoutMinutes), user.LockoutEnd);
    }

    [Fact]
    public async Task TheAttemptThatLocks_StillReportsInvalidCredentials()
    {
        // Answering "account locked" here would confirm the address exists to someone
        // who has just guessed wrong five times. The 403 starts on the next attempt.
        _ = SeedUser();

        await FailTimesAsync(maxAttempts - 1);
        LoginResult result = await AttemptAsync(correct: false);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task ALockedAccount_IsRejectedEvenWithTheCorrectPassword()
    {
        _ = SeedUser();
        await FailTimesAsync(maxAttempts);

        LoginResult result = await AttemptAsync(correct: true);

        Assert.Equal(LoginOutcome.AccountLocked, result.Outcome);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task AttemptsDuringALockout_DoNotExtendIt()
    {
        User user = SeedUser();
        await FailTimesAsync(maxAttempts);
        DateTime lockedUntil = user.LockoutEnd!.Value;

        // Time moves on, but still inside the window.
        timeProvider.SetUtcNow(now.AddMinutes(5));
        await FailTimesAsync(3);

        Assert.Equal(lockedUntil, user.LockoutEnd);
        Assert.Equal(maxAttempts, user.FailedLoginAttempts);
    }

    [Fact]
    public async Task ExactlyOneAlertIsRaisedPerLockout()
    {
        _ = SeedUser();

        await FailTimesAsync(maxAttempts + 4);

        AccountLockedEvent raised = Assert.Single(lockedEvents.PublishedEvents);

        Assert.Equal(knownEmail, raised.Email);
        Assert.Equal(maxAttempts, raised.FailedAttempts);
        Assert.Equal(now.AddMinutes(lockoutMinutes), raised.LockedUntilUtc);
    }

    [Fact]
    public async Task AnExpiredLockout_StartsANewSequenceAtOne()
    {
        User user = SeedUser();
        await FailTimesAsync(maxAttempts);

        timeProvider.SetUtcNow(now.AddMinutes(lockoutMinutes + 1));
        _ = await AttemptAsync(correct: false);

        Assert.Equal(1, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public async Task AnExpiredLockout_CanLockAgainAndAlertAgain()
    {
        _ = SeedUser();
        await FailTimesAsync(maxAttempts);

        timeProvider.SetUtcNow(now.AddMinutes(lockoutMinutes + 1));
        await FailTimesAsync(maxAttempts);

        Assert.Equal(2, lockedEvents.PublishedEvents.Count);
    }

    [Fact]
    public async Task ASuccessfulLogin_ClearsTheFailureCountAndLockout()
    {
        User user = SeedUser();
        await FailTimesAsync(maxAttempts - 1);

        LoginResult result = await AttemptAsync(correct: true);

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public async Task AnUnknownAddress_RecordsNothing()
    {
        // There is no row to count against, and inventing one would let an attacker
        // create records for addresses that were never registered.
        _ = await CreateService().LoginAsync(
            new LoginRequest { Email = "nobody@example.com", Password = "WrongPassword!1" });

        Assert.Empty(userRepository.FailedLoginCalls);
        Assert.Empty(lockedEvents.PublishedEvents);
    }

    [Fact]
    public async Task ThePolicyFromConfigurationIsWhatGetsApplied()
    {
        _ = SeedUser();

        _ = await AttemptAsync(correct: false);

        (string userId, int max, TimeSpan duration, DateTime when) = Assert.Single(userRepository.FailedLoginCalls);

        Assert.Equal(maxAttempts, max);
        Assert.Equal(TimeSpan.FromMinutes(lockoutMinutes), duration);
        Assert.Equal(now, when);
        Assert.False(string.IsNullOrWhiteSpace(userId));
    }

    [Fact]
    public async Task NoAttemptIsRecordedWhileTheAccountIsAlreadyLocked()
    {
        // The service returns on the lockout check before reaching the password, so
        // the repository is never asked to count an attempt during a lockout.
        _ = SeedUser();
        await FailTimesAsync(maxAttempts);
        int callsAfterLock = userRepository.FailedLoginCalls.Count;

        await FailTimesAsync(3);

        Assert.Equal(callsAfterLock, userRepository.FailedLoginCalls.Count);
    }

    [Fact]
    public async Task ALockoutLandingDuringASuccessfulLogin_RefusesTheSignIn()
    {
        // The race the reviewer identified: the user is read while unlocked, a
        // concurrent wrong password locks the account, and the successful request is
        // still holding the stale row. It must not clear that lock or hand out tokens.
        User user = SeedUser();

        userRepository.OnCompleteSuccessfulLogin = () =>
        {
            user.FailedLoginAttempts = maxAttempts;
            user.LockoutEnd = now.AddMinutes(lockoutMinutes);
        };

        LoginResult result = await AttemptAsync(correct: true);

        Assert.Equal(LoginOutcome.AccountLocked, result.Outcome);
        Assert.Null(result.Response);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task ALockoutLandingDuringASuccessfulLogin_IsNotCleared()
    {
        User user = SeedUser();
        DateTime lockedUntil = now.AddMinutes(lockoutMinutes);

        userRepository.OnCompleteSuccessfulLogin = () =>
        {
            user.FailedLoginAttempts = maxAttempts;
            user.LockoutEnd = lockedUntil;
        };

        _ = await AttemptAsync(correct: true);

        // Previously the tracked entity would have written lockout_end back to null.
        Assert.Equal(lockedUntil, user.LockoutEnd);
        Assert.Equal(maxAttempts, user.FailedLoginAttempts);
    }

    [Fact]
    public async Task TheResetIsCheckedBeforeAnyTokenIsIssued()
    {
        // Ordering matters: issuing first and checking afterwards would leak a usable
        // token for an account that turned out to be locked.
        _ = SeedUser();

        _ = await AttemptAsync(correct: true);

        Assert.Equal(1, userRepository.CompleteSuccessfulLoginCallCount);
        _ = Assert.Single(tokenService.Issued);
    }

    [Fact]
    public async Task ASuccessfulLogin_RecordsLastSignInThroughTheConditionalWrite()
    {
        User user = SeedUser();
        user.FailedLoginAttempts = 3;

        _ = await AttemptAsync(correct: true);

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
        Assert.Equal(now, user.LastLoginAt);
    }

    [Fact]
    public async Task ALockedAccount_StillSpendsAHashVerification()
    {
        // Same response is not enough on its own: returning early would make a locked
        // account answer in milliseconds while an unknown address and a wrong password
        // each cost a full BCrypt verification, and that gap re-opens the enumeration
        // signal by timing. The verify is against the dummy hash, so it also cannot
        // reveal whether the submitted password was correct.
        _ = SeedUser();
        await FailTimesAsync(maxAttempts);

        int verificationsBefore = passwordHasher.VerifiedPairs.Count;

        LoginResult result = await AttemptAsync(correct: true);

        Assert.Equal(LoginOutcome.AccountLocked, result.Outcome);

        (string _, string hash) = passwordHasher.VerifiedPairs[verificationsBefore];

        Assert.Equal(FakePasswordHasher.DummyHashValue, hash);
    }

    [Fact]
    public async Task EveryFailurePath_SpendsExactlyOneHashVerification()
    {
        // Unknown address, wrong password and locked account must all cost the same.
        AccountLockoutTests unknown = new();
        _ = await unknown.CreateService().LoginAsync(
            new LoginRequest { Email = "nobody@example.com", Password = "WrongPassword!1" });

        AccountLockoutTests wrong = new();
        _ = wrong.SeedUser();
        _ = await wrong.AttemptAsync(correct: false);

        AccountLockoutTests locked = new();
        _ = locked.SeedUser();
        await locked.FailTimesAsync(maxAttempts);
        int before = locked.passwordHasher.VerifiedPairs.Count;
        _ = await locked.AttemptAsync(correct: true);

        _ = Assert.Single(unknown.passwordHasher.VerifiedPairs);
        _ = Assert.Single(wrong.passwordHasher.VerifiedPairs);
        Assert.Equal(1, locked.passwordHasher.VerifiedPairs.Count - before);
    }
}
