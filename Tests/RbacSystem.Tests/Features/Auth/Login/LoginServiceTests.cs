using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RbacSystem.Application.Common.Configuration;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Features.Auth.Login;

/// <summary>
/// Behavioural tests for <see cref="LoginService"/>, covering each acceptance
/// criterion of the login feature.
/// </summary>
public class LoginServiceTests
{
    private const string knownEmail = "ada@example.com";
    private const string correctPassword = "Str0ng!Passw0rd";
    private const string storedHash = "$2a$12$storedhashvalue";

    private static readonly DateTime now = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRepository userRepository = new();
    private readonly FakePasswordHasher passwordHasher = new();
    private readonly FakeTokenService tokenService = new();
    private readonly FakeTimeProvider timeProvider = new(now);
    private readonly RecordingAccountLockedEventPublisher lockedEvents = new();
    private readonly AccountLockoutOptions lockoutPolicy = new() { MaxFailedAttempts = 5, DurationMinutes = 15 };

    private LoginService CreateService()
    {
        return new LoginService(
            userRepository,
            passwordHasher,
            tokenService,
            lockedEvents,
            Options.Create(lockoutPolicy),
            timeProvider);
    }

    private User SeedUser(
        bool verified = true,
        DateTime? lockoutEnd = null,
        string? passwordHash = storedHash,
        string email = knownEmail,
        UserStatus? status = null)
    {
        User user = new()
        {
            Email = email,
            Name = "ada",
            PasswordHash = passwordHash,
            Role = UserRole.User,
            Status = status ?? (verified ? UserStatus.Active : UserStatus.PendingVerification),
            EmailVerifiedAt = verified ? now.AddDays(-1) : null,
            LockoutEnd = lockoutEnd
        };

        userRepository.SeedUser(user);

        return user;
    }

    private static LoginRequest Request(string email = knownEmail, string password = correctPassword)
    {
        return new LoginRequest { Email = email, Password = password };
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_ForValidCredentials()
    {
        _ = SeedUser();

        LoginResult result = await CreateService().LoginAsync(Request());

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.NotNull(result.Response);
        Assert.Equal("access-token", result.Response.AccessToken);
        Assert.Equal("refresh-token", result.Response.RefreshToken);
        Assert.Equal("Bearer", result.Response.TokenType);
        Assert.Equal(900, result.Response.ExpiresIn);
    }

    [Fact]
    public async Task LoginAsync_RejectsUnknownEmail()
    {
        LoginResult result = await CreateService().LoginAsync(Request("nobody@example.com"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Null(result.Response);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_RejectsWrongPassword()
    {
        _ = SeedUser();
        passwordHasher.VerifyResult = false;

        LoginResult result = await CreateService().LoginAsync(Request(password: "WrongPassword!1"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_ReportsUnknownEmailAndWrongPasswordIdentically()
    {
        // The two must be indistinguishable to the caller, or the endpoint becomes an
        // account-enumeration oracle.
        LoginResult unknownEmail = await CreateService().LoginAsync(Request("nobody@example.com"));

        LoginServiceTests wrongPasswordCase = new();
        _ = wrongPasswordCase.SeedUser();
        wrongPasswordCase.passwordHasher.VerifyResult = false;
        LoginResult wrongPassword = await wrongPasswordCase.CreateService().LoginAsync(Request());

        Assert.Equal(unknownEmail.Outcome, wrongPassword.Outcome);
        Assert.Equal(unknownEmail.Response, wrongPassword.Response);
    }

    [Fact]
    public async Task LoginAsync_VerifiesAgainstDummyHash_WhenEmailIsUnknown()
    {
        // Skipping the hash for a missing account would make the miss measurably
        // faster than a real password check.
        _ = await CreateService().LoginAsync(Request("nobody@example.com"));

        (string _, string hash) = Assert.Single(passwordHasher.VerifiedPairs);

        Assert.Equal(FakePasswordHasher.DummyHashValue, hash);
    }

    [Fact]
    public async Task LoginAsync_VerifiesAgainstDummyHash_ForOAuthOnlyAccount()
    {
        // A user with no password must not throw, and must not be distinguishable
        // from an address that was never registered.
        _ = SeedUser(passwordHash: null);

        LoginResult result = await CreateService().LoginAsync(Request());

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Equal(
            FakePasswordHasher.DummyHashValue,
            Assert.Single(passwordHasher.VerifiedPairs).Hash);
    }

    [Fact]
    public async Task LoginAsync_RejectsUnverifiedAccount()
    {
        _ = SeedUser(verified: false);

        LoginResult result = await CreateService().LoginAsync(Request());

        Assert.Equal(LoginOutcome.EmailNotVerified, result.Outcome);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_RejectsLockedAccount_EvenWithCorrectPassword()
    {
        _ = SeedUser(lockoutEnd: now.AddMinutes(5));

        LoginResult result = await CreateService().LoginAsync(Request());

        Assert.Equal(LoginOutcome.AccountLocked, result.Outcome);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_ChecksLockoutBeforePassword()
    {
        // A locked account must not reveal whether the supplied password was right.
        _ = SeedUser(lockoutEnd: now.AddMinutes(5));
        passwordHasher.VerifyResult = false;

        LoginResult result = await CreateService().LoginAsync(Request(password: "WrongPassword!1"));

        Assert.Equal(LoginOutcome.AccountLocked, result.Outcome);
        Assert.Empty(passwordHasher.VerifiedPairs);
    }

    [Fact]
    public async Task LoginAsync_AllowsLogin_OnceLockoutHasExpired()
    {
        _ = SeedUser(lockoutEnd: now.AddMinutes(-1));

        LoginResult result = await CreateService().LoginAsync(Request());

        Assert.Equal(LoginOutcome.Success, result.Outcome);
    }

    [Theory]
    [InlineData("ada@example.com")]
    [InlineData("ADA@EXAMPLE.COM")]
    [InlineData("  Ada@Example.Com  ")]
    public async Task LoginAsync_NormalizesEmailBeforeLookup(string submitted)
    {
        _ = SeedUser();

        LoginResult result = await CreateService().LoginAsync(Request(submitted));

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.Equal(knownEmail, Assert.Single(userRepository.GetByEmailArguments));
    }

    [Fact]
    public async Task LoginAsync_RecordsLastLoginTimestampAndAddress()
    {
        User user = SeedUser();
        var address = IPAddress.Parse("203.0.113.7");

        _ = await CreateService().LoginAsync(Request(), "curl/8.0", address);

        Assert.Equal(now, user.LastLoginAt);
        Assert.Equal(address, user.LastLoginIp);
        Assert.Equal(1, userRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task LoginAsync_DoesNotPersist_WhenAuthenticationFails()
    {
        _ = SeedUser();
        passwordHasher.VerifyResult = false;

        _ = await CreateService().LoginAsync(Request());

        Assert.Equal(0, userRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task LoginAsync_PassesUserAgentAndAddressToTokenService()
    {
        _ = SeedUser();
        var address = IPAddress.Parse("203.0.113.7");

        _ = await CreateService().LoginAsync(Request(), "curl/8.0", address);

        (User _, string _, string? userAgent, IPAddress? ipAddress, string? rotatedFromId) =
            Assert.Single(tokenService.Issued);

        Assert.Equal("curl/8.0", userAgent);
        Assert.Equal(address, ipAddress);
        Assert.Null(rotatedFromId);
    }

    [Fact]
    public async Task LoginAsync_StartsANewSessionFamilyPerLogin()
    {
        _ = SeedUser();
        LoginService service = CreateService();

        _ = await service.LoginAsync(Request());
        _ = await service.LoginAsync(Request());

        Assert.Equal(2, tokenService.Issued.Count);
        Assert.NotEqual(tokenService.Issued[0].TokenFamily, tokenService.Issued[1].TokenFamily);
        Assert.All(tokenService.Issued, entry => Assert.False(string.IsNullOrWhiteSpace(entry.TokenFamily)));
    }

    [Theory]
    [InlineData(UserStatus.Inactive)]
    [InlineData(UserStatus.Suspended)]
    public async Task LoginAsync_RejectsBlockedAccount(UserStatus status)
    {
        _ = SeedUser(status: status);

        LoginResult result = await CreateService().LoginAsync(Request());

        Assert.Equal(LoginOutcome.AccountNotActive, result.Outcome);
        Assert.Null(result.Response);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_AllowsAnActiveAccount()
    {
        _ = SeedUser(status: UserStatus.Active);

        Assert.Equal(LoginOutcome.Success, (await CreateService().LoginAsync(Request())).Outcome);
    }

    [Fact]
    public async Task LoginAsync_ChecksStatusAfterPassword()
    {
        // A wrong password against a suspended account must look like any other bad
        // credential, or the status becomes an enumeration signal for free.
        _ = SeedUser(status: UserStatus.Suspended);
        passwordHasher.VerifyResult = false;

        LoginResult result = await CreateService().LoginAsync(Request(password: "WrongPassword!1"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task LoginAsync_DoesNotPersistOrIssueTokens_ForABlockedAccount()
    {
        _ = SeedUser(status: UserStatus.Suspended);

        _ = await CreateService().LoginAsync(Request());

        Assert.Equal(0, userRepository.SaveChangesCallCount);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_TreatsAMissingUserAsInvalidCredentials_WhichIsHowSoftDeletedUsersArrive()
    {
        // The global query filter keeps soft-deleted rows out of the lookup, so the
        // service sees them exactly as it sees an address that was never registered.
        LoginResult result = await CreateService().LoginAsync(Request("deleted@example.com"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Empty(tokenService.Issued);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenRequestIsNull()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateService().LoginAsync(null!));
    }
}
