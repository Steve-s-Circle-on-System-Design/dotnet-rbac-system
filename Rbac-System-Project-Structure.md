# RBAC System Project Structure

## Current state

The repository currently provides a Clean Architecture scaffold. Domain entities,
authentication use cases, repositories, entity configurations, and migrations will
be introduced with their related features rather than represented by placeholders.

```text
dotnet-rbac-system/
|-- src/
|   |-- RbacSystem.Domain/
|   |-- RbacSystem.Application/
|   |   `-- DependencyInjection.cs
|   |-- RbacSystem.Infrastructure/
|   |   |-- Persistence/AppDbContext.cs
|   |   `-- DependencyInjection.cs
|   `-- RbacSystem.API/
|       |-- Controllers/HealthController.cs
|       |-- Program.cs
|       |-- appsettings.json
|       `-- appsettings.Development.json
`-- Tests/RbacSystem.Tests/
```

## Dependency rules

```text
API ---------> Application ---------> Domain
 |                                      ^
 `----------> Infrastructure ----------|
                    |
                    `-----> Application
```

- `Domain` contains entities, value objects, enums, and domain rules. It has no
  dependencies on the other projects or on persistence frameworks.
- `Application` contains use cases and abstractions for persistence or external
  services. It depends only on Domain.
- `Infrastructure` implements Application abstractions and contains EF Core,
  PostgreSQL, repositories, migrations, and external integrations.
- `API` is the composition root. It handles HTTP concerns and wires Application
  and Infrastructure together.
- `Tests` verifies behavior across the appropriate layers.

## Planned request flow

```text
HTTP request
  -> API controller
  -> Application use case/service
  -> Application repository abstraction
  -> Infrastructure repository
  -> AppDbContext
  -> PostgreSQL
```

Controllers must remain thin. Business decisions belong in Application or Domain,
and EF Core queries belong in Infrastructure.

## Planned feature placement

```text
RbacSystem.Domain/
|-- Common/
|-- Entities/
`-- Enums/

RbacSystem.Application/
|-- Features/
|-- Interfaces/Repositories/
`-- Interfaces/Services/

RbacSystem.Infrastructure/
|-- Persistence/
|   |-- Configurations/
|   `-- Migrations/
|-- Repositories/
`-- Services/

RbacSystem.API/
|-- Controllers/
|-- Middleware/
`-- Authorization/
```

Folders should be added only when their feature is implemented. Empty placeholder
classes are not required.

## Agreed persistence direction

The future schema will contain `users`, `otp_verifications`, `email_logs`,
`audit_logs`, `refresh_tokens`, `password_resets`, and `files`.

- Identifiers are canonical lowercase UUID v4 strings stored as `varchar(36)`.
- Roles are the fixed `User` and `Admin` values, not role/permission tables.
- Emails use PostgreSQL `citext` where case-insensitive comparison is required.
- Passwords, OTPs, refresh tokens, and password-reset tokens are stored only as
  hashes.
- Security timestamps use UTC and PostgreSQL `timestamp with time zone`.
- Entity mappings belong in individual Infrastructure configuration classes.

These decisions describe the target implementation; the scaffold must not claim
that the schema exists before its migration is added.

## Authentication direction

The planned authentication system supports email/password, Google OAuth, and
six-digit magic OTP login. Access tokens expire after 15 minutes. Refresh tokens
expire after seven days, are single-use, rotate within a token family, and support
multiple device sessions. Five consecutive failed logins lock an account for 15
minutes. Authorization uses fixed `User` and `Admin` roles through ASP.NET Core
authorization policies or attributes.

The JWT package and authentication middleware may exist in the scaffold, but JWT
issuer, audience, signing credentials, Swagger security, and validation options
must be added only with the authentication implementation.

## Configuration and secrets

Tracked configuration may contain non-secret defaults. Connection strings, JWT
signing keys, OAuth client secrets, email-provider secrets, and Cloudinary secrets
must never be committed.

For local development, use .NET user secrets on the API project. Production must
provide the same configuration keys through environment variables or a managed
secret store.

## Migrations

EF Core migrations define the shared schema and must be committed. Each developer's
database data and `__EFMigrationsHistory` rows remain local.

Schema-changing work must:

1. Synchronize with the target branch before generating a migration.
2. Commit the migration, designer file, and model snapshot with the feature.
3. Review both `Up()` and `Down()`.
4. Apply migrations to a disposable PostgreSQL database and verify rollback.
5. Never edit a migration after it has reached a shared branch or environment;
   add a corrective migration instead.

