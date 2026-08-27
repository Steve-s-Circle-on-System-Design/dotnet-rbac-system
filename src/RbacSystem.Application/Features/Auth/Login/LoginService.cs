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
        // credential correctness. Setting LockoutEnd belongs to the failed-attempt
        // tracking feature; this only enforces a lock already in place.
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
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

        user.LastLoginAt = now;
        user.LastLoginIp = ipAddress;

        // A successful sign-in ends the current failure sequence. This one is safe on
        // the tracked entity rather than needing the atomic statement, because
        // resetting to zero does not depend on the value it replaces.
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

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
