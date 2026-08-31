using System.Net;
using Microsoft.Extensions.Options;
using RbacSystem.Application.Common.Configuration;
using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Domain.Common;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Application.Features.Auth.Login;

/// <inheritdoc cref="ILoginService" />
public sealed class LoginService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAccountLockedEventPublisher accountLockedEventPublisher,
    IOptions<AccountLockoutOptions> lockoutOptions,
    TimeProvider timeProvider) : ILoginService
{
    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        string? userAgent = null,
        IPAddress? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Same normalization registration applies, so the lookup matches how the
        // address was stored.
        string email = request.Email.Trim().ToLowerInvariant();

        User? user = await userRepository.GetByEmailAsync(email, cancellationToken);

        // An account with no password is an OAuth-only account: it cannot be signed
        // into with a credential pair, and must burn the same time as a real miss.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            _ = passwordHasher.Verify(request.Password, passwordHasher.DummyHash);
            return LoginResult.Failed(LoginOutcome.InvalidCredentials);
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        // Checked before the password so a locked account cannot be probed for
        // credential correctness.
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
            // Verified against the dummy hash purely to spend the same time as the
            // other two failure paths. Returning early would make a locked account
            // answer in milliseconds while an unknown address and a wrong password
            // each cost a full BCrypt verification, and that difference re-opens by
            // timing exactly the enumeration signal that giving all three the same
            // response closes. It costs no more than any other wrong password does.
            _ = passwordHasher.Verify(request.Password, passwordHasher.DummyHash);

            return LoginResult.Failed(LoginOutcome.AccountLocked);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await RecordFailedAttemptAsync(user, now, cancellationToken);

            // Still InvalidCredentials even on the attempt that trips the lock: the
            // password really was wrong, and answering "account locked" here would
            // confirm the address exists to someone who has just guessed wrong five
            // times. The lockout response begins on the next attempt.
            return LoginResult.Failed(LoginOutcome.InvalidCredentials);
        }

        if (!user.IsEmailVerified)
        {
            return LoginResult.Failed(LoginOutcome.EmailNotVerified);
        }

        // Checked after the password for the same reason as the verification gate:
        // only a caller who has already proved they know the password learns that an
        // account is blocked. PendingVerification is deliberately absent — new
        // registrations carry that status until email verification promotes them, so
        // rejecting it here would lock out every account created so far.
        if (user.Status is UserStatus.Inactive or UserStatus.Suspended)
        {
            return LoginResult.Failed(LoginOutcome.AccountNotActive);
        }

        // Soft-deleted users never reach this point: a global query filter keeps them
        // out of the lookup entirely, so they fail as though the address is unknown.

        // Clearing the failure state through the change tracker would be unsafe: a
        // concurrent failed attempt can lock the account between the read above and
        // the save below, and the stale tracked entity would then write lockout_end
        // back to null, cancelling that lock. This does the check and the write in
        // one statement, and runs before any token is issued so a sign-in that lost
        // the race cannot walk away with credentials.
        if (!await userRepository.TryCompleteSuccessfulLoginAsync(user.Id, now, ipAddress, cancellationToken))
        {
            return LoginResult.Failed(LoginOutcome.AccountLocked);
        }

        // A fresh family per login, so each session rotates independently once
        // refresh-token rotation is implemented.
        IssuedTokens tokens = await tokenService.IssueTokenPairAsync(
            user,
            EntityId.New(),
            userAgent,
            ipAddress,
            cancellationToken: cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);

        return LoginResult.Success(new LoginResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            "Bearer",
            tokens.AccessTokenExpiresInSeconds));
    }

    /// <summary>
    /// Records a failed attempt and raises the alert if it started a lockout.
    /// </summary>
    /// <remarks>
    /// The event fires only on the transition into a lockout, so an attacker
    /// hammering an already locked account cannot flood the owner with alerts.
    /// </remarks>
    private async Task RecordFailedAttemptAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        AccountLockoutOptions policy = lockoutOptions.Value;

        FailedLoginOutcome outcome = await userRepository.RegisterFailedLoginAsync(
            user.Id,
            policy.MaxFailedAttempts,
            policy.Duration,
            now,
            cancellationToken);

        if (!outcome.LockoutJustStarted || outcome.LockoutEnd is not { } lockedUntil)
        {
            return;
        }

        await accountLockedEventPublisher.PublishAsync(
            new AccountLockedEvent(user.Id, user.Email, outcome.FailedAttempts, lockedUntil, now),
            cancellationToken);
    }
}
