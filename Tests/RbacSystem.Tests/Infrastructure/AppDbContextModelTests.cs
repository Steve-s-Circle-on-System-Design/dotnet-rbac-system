using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RbacSystem.Domain.Entities;
using RbacSystem.Infrastructure.Persistence;

namespace RbacSystem.Tests.Infrastructure;

/// <summary>
/// Asserts model configuration that carries security weight.
/// </summary>
/// <remarks>
/// Builds the EF model without opening a connection, so these run anywhere. A
/// database round trip would prove the same thing but only where PostgreSQL is
/// available, which would mean the guard silently stops running in CI.
/// </remarks>
public class AppDbContextModelTests
{
    private static AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=none;Password=none")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void User_HasASoftDeleteQueryFilter()
    {
        // Without this filter, deleted accounts are returned by ordinary queries and
        // can authenticate. Enforcing it on the model means every feature inherits
        // the rule instead of each one remembering to filter.
        using AppDbContext context = CreateContext();

        IEntityType user = context.Model.FindEntityType(typeof(User))!;

        Assert.NotNull(user.GetQueryFilter());
        Assert.Contains("DeletedAt", user.GetQueryFilter()!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void User_StillHasItsUniqueEmailIndex()
    {
        // The unique index spans deleted rows too, so re-registering a soft-deleted
        // address is rejected by the database even though the pre-check no longer
        // sees it.
        using AppDbContext context = CreateContext();

        IEntityType user = context.Model.FindEntityType(typeof(User))!;

        Assert.Contains(
            user.GetIndexes(),
            index => index.IsUnique && index.GetDatabaseName() == "ux_users_email");
    }
}
