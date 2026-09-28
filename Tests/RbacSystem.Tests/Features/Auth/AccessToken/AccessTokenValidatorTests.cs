using RbacSystem.Application.Features.Auth.AccessToken;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Features.Auth.AccessToken;

/// <summary>Tests access-token claims against current account security state.</summary>
public sealed class AccessTokenValidatorTests
{
    private readonly FakeUserRepository users = new();

    private AccessTokenValidator CreateValidator()
    {
        return new AccessTokenValidator(users);
    }

    private User SeedUser(UserStatus status = UserStatus.Active, int tokenVersion = 0)
    {
        User user = new()
        {
            Email = "ada@example.com",
            Name = "ada",
            Status = status,
            Role = UserRole.User,
            TokenVersion = tokenVersion
        };

        users.SeedUser(user);
        return user;
    }

    [Fact]
    public async Task IsValidAsync_AcceptsANewUsersMatchingVersionZero()
    {
        User user = SeedUser();

        bool result = await CreateValidator().IsValidAsync(user.Id, 0);

        Assert.True(result);
    }

    [Fact]
    public async Task IsValidAsync_RejectsAnOutdatedVersion()
    {
        User user = SeedUser(tokenVersion: 1);

        bool result = await CreateValidator().IsValidAsync(user.Id, 0);

        Assert.False(result);
    }

    [Fact]
    public async Task IsValidAsync_RejectsAMissingOrSoftDeletedUser()
    {
        bool result = await CreateValidator().IsValidAsync("missing-user", 0);

        Assert.False(result);
    }

    [Theory]
    [InlineData(UserStatus.Inactive)]
    [InlineData(UserStatus.Suspended)]
    public async Task IsValidAsync_RejectsBlockedAccounts(UserStatus status)
    {
        User user = SeedUser(status);

        bool result = await CreateValidator().IsValidAsync(user.Id, 0);

        Assert.False(result);
    }

    [Fact]
    public async Task IsValidAsync_AllowsPendingVerificationUnderTheTemporaryProjectRule()
    {
        User user = SeedUser(UserStatus.PendingVerification);

        bool result = await CreateValidator().IsValidAsync(user.Id, 0);

        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsValidAsync_RejectsAMissingUserIdWithoutQuerying(string userId)
    {
        bool result = await CreateValidator().IsValidAsync(userId, 0);

        Assert.False(result);
        Assert.Empty(users.SecurityStateArguments);
    }

    [Fact]
    public async Task IsValidAsync_RejectsANegativeVersionWithoutQuerying()
    {
        bool result = await CreateValidator().IsValidAsync("user-1", -1);

        Assert.False(result);
        Assert.Empty(users.SecurityStateArguments);
    }

    [Fact]
    public async Task IsValidAsync_ForwardsUserIdAndCancellationToTheRepository()
    {
        User user = SeedUser();
        using var cancellation = new CancellationTokenSource();

        _ = await CreateValidator().IsValidAsync(user.Id, 0, cancellation.Token);

        (string userId, CancellationToken token) = Assert.Single(users.SecurityStateArguments);
        Assert.Equal(user.Id, userId);
        Assert.Equal(cancellation.Token, token);
    }
}
