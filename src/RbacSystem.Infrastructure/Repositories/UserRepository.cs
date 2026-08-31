using System.Data;
using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    /// <summary>
    /// Records one failed attempt, resetting an expired sequence, incrementing the
    /// live one, and applying the lock — all in a single statement.
    /// </summary>
    /// <remarks>
    /// The WHERE clause skips a row whose lockout is still active, so an attempt made
    /// during a lockout changes nothing and cannot push the expiry further out. The
    /// nested CASE restarts the count at 1 when the previous lockout has expired,
    /// rather than resuming from the value that caused it. RETURNING hands back the
    /// post-update state so the caller learns whether this attempt is the one that
    /// applied the lock without issuing a second, racy read.
    /// </remarks>
    private const string registerFailedLoginSql = """
        UPDATE users SET
            failed_login_attempts = CASE
                WHEN lockout_end IS NOT NULL AND lockout_end <= @now THEN 1
                ELSE failed_login_attempts + 1
            END,
            lockout_end = CASE
                WHEN (CASE
                        WHEN lockout_end IS NOT NULL AND lockout_end <= @now THEN 1
                        ELSE failed_login_attempts + 1
                      END) >= @max_attempts
                THEN @now + @lockout_duration
                ELSE NULL
            END,
            updated_at = @now
        WHERE id = @id
          AND deleted_at IS NULL
          AND (lockout_end IS NULL OR lockout_end <= @now)
        RETURNING failed_login_attempts, lockout_end;
        """;

    /// <summary>
    /// Clears the failure state for a successful sign-in, refusing to do so if a
    /// lockout is active at that instant.
    /// </summary>
    /// <remarks>
    /// The lockout predicate lives in the WHERE clause rather than in application
    /// code, so the check and the write cannot be separated by a concurrent failed
    /// attempt. Last-sign-in fields are written here too, so the login flow never
    /// mutates the tracked entity and EF cannot flush a stale lockout value over the
    /// top of a lock applied in between.
    /// </remarks>
    private const string completeSuccessfulLoginSql = """
        UPDATE users SET
            failed_login_attempts = 0,
            lockout_end = NULL,
            last_login_at = @now,
            last_login_ip = @ip,
            updated_at = @now
        WHERE id = @id
          AND deleted_at IS NULL
          AND (lockout_end IS NULL OR lockout_end <= @now);
        """;

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        // users.email is citext, so this comparison is case-insensitive in the database.
        return await context.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Tracked, because the login flow updates the last-login fields on the
        // returned entity. users.email is citext, so the match is case-insensitive.
        return await context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FailedLoginOutcome> RegisterFailedLoginAsync(
        string userId,
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFailedAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lockoutDuration, TimeSpan.Zero);

        // lockout_end is timestamptz, and Npgsql rejects a non-UTC DateTime for it.
        // Failing here names the caller's mistake instead of surfacing an opaque
        // provider error from inside the command.
        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"The current time must be UTC, but was {nowUtc.Kind}.", nameof(nowUtc));
        }

        // Raw ADO over EF's own connection rather than a LINQ read-modify-write.
        // ExecuteUpdate cannot return the updated row, and a query filter would hide
        // the RETURNING clause this depends on; going through the DbContext keeps the
        // same connection, and every value is a parameter.
        DbConnection connection = context.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using DbCommand command = connection.CreateCommand();

            command.CommandText = registerFailedLoginSql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            AddParameter(command, "id", userId);
            AddParameter(command, "now", nowUtc);
            AddParameter(command, "max_attempts", maxFailedAttempts);
            AddParameter(command, "lockout_duration", lockoutDuration);

            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            // No row means the lockout was already active, so this attempt was not
            // counted and must not raise a second alert.
            if (!await reader.ReadAsync(cancellationToken))
            {
                return FailedLoginOutcome.AlreadyLocked(null);
            }

            int failedAttempts = reader.GetInt32(0);
            DateTime? lockoutEnd = await reader.IsDBNullAsync(1, cancellationToken)
                ? null
                : reader.GetDateTime(1);

            // The WHERE clause excluded rows that were already locked, so a lockout
            // in the future can only have been written by this statement.
            bool lockoutJustStarted = lockoutEnd is { } end && end > nowUtc;

            return new FailedLoginOutcome(failedAttempts, lockoutEnd, lockoutJustStarted);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryCompleteSuccessfulLoginAsync(
        string userId,
        DateTime nowUtc,
        IPAddress? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"The current time must be UTC, but was {nowUtc.Kind}.", nameof(nowUtc));
        }

        DbConnection connection = context.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using DbCommand command = connection.CreateCommand();

            command.CommandText = completeSuccessfulLoginSql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            AddParameter(command, "id", userId);
            AddParameter(command, "now", nowUtc);
            AddParameter(command, "ip", (object?)ipAddress ?? DBNull.Value);

            // Zero rows means a concurrent failure locked the account after it was
            // loaded, so this sign-in must not proceed.
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value;

        _ = command.Parameters.Add(parameter);
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
