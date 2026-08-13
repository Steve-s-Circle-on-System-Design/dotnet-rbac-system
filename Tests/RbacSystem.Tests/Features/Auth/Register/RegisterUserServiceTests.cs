using RbacSystem.Application.Features.Auth.Register;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Tests.Fakes;

namespace RbacSystem.Tests.Features.Auth.Register;

/// <summary>
/// Behavioural tests for <see cref="RegisterUserService"/>, covering each acceptance
/// criterion of the registration feature.
/// </summary>
public class RegisterUserServiceTests
{
    private const string validPassword = "Str0ng!Passw0rd";

    private readonly FakeUserRepository userRepository = new();
    private readonly FakePasswordHasher passwordHasher = new();
    private readonly RecordingUserRegisteredEventPublisher eventPublisher = new();

    private RegisterUserService CreateService()
    {
        return new RegisterUserService(userRepository, passwordHasher, eventPublisher);
    }

    private static RegisterRequest Request(string email, string password = validPassword)
    {
        return new RegisterRequest { Email = email, Password = password };
    }

    [Fact]
    public async Task RegisterAsync_CreatesUser_ForNewEmail()
    {
        RegisterResult result = await CreateService().RegisterAsync(Request("ada@example.com"));

        Assert.Equal(RegisterResult.Success, result);
        _ = Assert.Single(userRepository.AddedUsers);
    }

    [Theory]
    [InlineData("ada@example.com", "ada@example.com")]
    [InlineData("ADA@EXAMPLE.COM", "ada@example.com")]
    [InlineData("  Ada@Example.Com  ", "ada@example.com")]
    public async Task RegisterAsync_NormalizesEmail(string submitted, string expected)
    {
        _ = await CreateService().RegisterAsync(Request(submitted));

        Assert.Equal(expected, userRepository.AddedUsers.Single().Email);
    }

    [Theory]
    [InlineData("ada@example.com", "ada")]
    [InlineData("Ada.Lovelace@Example.com", "ada.lovelace")]
    public async Task RegisterAsync_DerivesNameFromEmailLocalPart(string email, string expectedName)
    {
        _ = await CreateService().RegisterAsync(Request(email));

        Assert.Equal(expectedName, userRepository.AddedUsers.Single().Name);
    }

    [Fact]
    public async Task RegisterAsync_AssignsDefaultUserRole()
    {
        _ = await CreateService().RegisterAsync(Request("ada@example.com"));

        Assert.Equal(UserRole.User, userRepository.AddedUsers.Single().Role);
    }

    [Fact]
    public async Task RegisterAsync_SavesUserAsUnverified()
    {
        _ = await CreateService().RegisterAsync(Request("ada@example.com"));

        User created = userRepository.AddedUsers.Single();

        Assert.Equal(UserStatus.PendingVerification, created.Status);
        Assert.Null(created.EmailVerifiedAt);
        Assert.False(created.IsEmailVerified);
    }

    [Fact]
    public async Task RegisterAsync_StoresHashedPassword_NeverThePlaintext()
    {
        _ = await CreateService().RegisterAsync(Request("ada@example.com"));

        string? storedHash = userRepository.AddedUsers.Single().PasswordHash;

        Assert.Equal(validPassword, Assert.Single(passwordHasher.HashedPasswords));
        Assert.NotNull(storedHash);
        Assert.NotEqual(validPassword, storedHash);
        Assert.DoesNotContain(validPassword, storedHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_AssignsIdAndCreationTimestamp()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        _ = await CreateService().RegisterAsync(Request("ada@example.com"));

        User created = userRepository.AddedUsers.Single();

        Assert.True(Guid.TryParse(created.Id, out _));
        Assert.InRange(created.CreatedAt, before, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task RegisterAsync_PublishesEventOnce_AfterSuccessfulInsert()
    {
        _ = await CreateService().RegisterAsync(Request("ada@example.com"));

        UserRegisteredEvent published = Assert.Single(eventPublisher.PublishedEvents);
        User created = userRepository.AddedUsers.Single();

        Assert.Equal(created.Id, published.UserId);
        Assert.Equal("ada@example.com", published.Email);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail_WithoutInsertingOrHashing()
    {
        userRepository.SeedExistingEmail("ada@example.com");

        RegisterResult result = await CreateService().RegisterAsync(Request("ada@example.com"));

        Assert.Equal(RegisterResult.DuplicateEmail, result);
        Assert.Empty(userRepository.AddedUsers);
        Assert.Equal(0, userRepository.TryAddCallCount);
        Assert.Empty(passwordHasher.HashedPasswords);
        Assert.Empty(eventPublisher.PublishedEvents);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicate_WhenSubmittedEmailDiffersOnlyByCase()
    {
        userRepository.SeedExistingEmail("ada@example.com");

        RegisterResult result = await CreateService().RegisterAsync(Request("ADA@Example.COM"));

        Assert.Equal(RegisterResult.DuplicateEmail, result);
        Assert.Empty(userRepository.AddedUsers);
    }

    [Fact]
    public async Task RegisterAsync_ChecksDuplicateUsingNormalizedEmail()
    {
        _ = await CreateService().RegisterAsync(Request("  ADA@Example.com "));

        Assert.Equal("ada@example.com", Assert.Single(userRepository.EmailExistsArguments));
    }

    [Fact]
    public async Task RegisterAsync_TreatsLostUniqueIndexRace_AsDuplicate()
    {
        // EmailExistsAsync reports the address is free, but the insert is then
        // rejected by the unique index because a concurrent request got there first.
        userRepository.RejectNextAdd = true;

        RegisterResult result = await CreateService().RegisterAsync(Request("ada@example.com"));

        Assert.Equal(RegisterResult.DuplicateEmail, result);
        Assert.Equal(1, userRepository.TryAddCallCount);
        Assert.Empty(userRepository.AddedUsers);
        Assert.Empty(eventPublisher.PublishedEvents);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenRequestIsNull()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateService().RegisterAsync(null!));
    }
}
