using Microsoft.EntityFrameworkCore;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;
using RbacSystem.Infrastructure.Persistence;

namespace RbacSystem.Tests.Database;

/// <summary>
/// Provides a real PostgreSQL database for tests that exercise SQL the in-memory
/// fakes cannot represent.
/// </summary>
/// <remarks>
/// The concurrency guarantee behind failed-login tracking lives in a single atomic
/// statement, so it can only be proven against a real server. CI always supplies
/// <c>ConnectionStrings__TestDatabase</c>; locally the tests skip with a clear
/// message when it is absent, so a contributor without Docker is not blocked while
/// CI coverage stays guaranteed.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>Environment variable holding the test connection string.</summary>
    public const string ConnectionStringVariable = "ConnectionStrings__TestDatabase";

    /// <summary>The connection string, or null when these tests should skip.</summary>
    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringVariable);

    /// <summary>Whether a database is available.</summary>
    public static bool IsAvailable => !string.IsNullOrWhiteSpace(ConnectionString);

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        // Migrate once for the whole class rather than per test.
        await using AppDbContext context = CreateContext();

        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a context. Each concurrent caller needs its own, since a DbContext is
    /// not thread-safe and the concurrency test deliberately runs many at once.
    /// </summary>
    public static AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Inserts a verified, active user with a unique address so tests never collide.
    /// </summary>
    public static async Task<User> SeedUserAsync(
        int failedAttempts = 0,
        DateTime? lockoutEnd = null,
        DateTime? deletedAt = null)
    {
        await using AppDbContext context = CreateContext();

        User user = new()
        {
            Email = $"lockout-{Guid.NewGuid():N}@example.com",
            Name = "lockout-test",
            PasswordHash = "$2a$12$notarealhashbutlongenoughtostore",
            Role = UserRole.User,
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow.AddDays(-1),
            FailedLoginAttempts = failedAttempts,
            LockoutEnd = lockoutEnd,
            DeletedAt = deletedAt
        };

        _ = context.Users.Add(user);
        _ = await context.SaveChangesAsync();

        return user;
    }

    /// <summary>Reads a user back, bypassing the soft-delete filter.</summary>
    public static async Task<User> ReloadAsync(string userId)
    {
        await using AppDbContext context = CreateContext();

        return await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(user => user.Id == userId);
    }
}

/// <summary>
/// A fact that skips itself when no test database is configured.
/// </summary>
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    /// <summary>Initializes the attribute, skipping when no database is available.</summary>
    public RequiresPostgresFactAttribute()
    {
        if (!PostgresFixture.IsAvailable)
        {
            Skip = $"Set {PostgresFixture.ConnectionStringVariable} to run database-backed tests.";
        }
    }
}
