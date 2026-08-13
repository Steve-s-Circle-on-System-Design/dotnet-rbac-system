using Microsoft.EntityFrameworkCore;
using Npgsql;
using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Domain.Entities;
using RbacSystem.Infrastructure.Persistence;

namespace RbacSystem.Infrastructure.Repositories;

/// <inheritdoc cref="IUserRepository" />
public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    /// <summary>PostgreSQL SQLSTATE for a unique-constraint violation.</summary>
    private const string uniqueViolationSqlState = "23505";

    /// <summary>Unique index protecting <c>users.email</c>.</summary>
    private const string emailUniqueIndexName = "ux_users_email";

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        // users.email is citext, so this comparison is case-insensitive in the database.
        return await context.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        _ = await context.Users.AddAsync(user, cancellationToken);

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
        {
            // Another request registered this address first. Detach the rejected
            // entity so the scoped context is not left holding an unsaved insert.
            context.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    /// <summary>
    /// Determines whether a failed save was caused by the unique email index rather
    /// than by some other constraint, which must not be swallowed.
    /// </summary>
    private static bool IsEmailUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == uniqueViolationSqlState
            && string.Equals(
                postgresException.ConstraintName,
                emailUniqueIndexName,
                StringComparison.Ordinal);
    }
}
