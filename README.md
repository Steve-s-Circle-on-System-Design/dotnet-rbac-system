# RBAC Auth System (.NET)

An open-source, multi-channel authentication and role-based authorization system
built with ASP.NET Core, Entity Framework Core, and PostgreSQL.

## Current state

This branch contains the Clean Architecture foundation:

- .NET 8 solution split into Domain, Application, Infrastructure, API, and Tests
- ASP.NET Core controllers and a basic health endpoint
- Swagger/OpenAPI
- EF Core and Npgsql registration
- Local configuration through .NET user secrets

Domain entities, repositories, database migrations, and authentication endpoints
will be added with the core implementation. The current scaffold intentionally does
not contain placeholder role, permission, or user models.

## Planned capabilities

- Email/password authentication with bcrypt hashing
- Google OAuth 2.0 with secure account linking
- Six-digit passwordless OTP authentication
- 15-minute JWT access tokens
- Single-use, rotating seven-day refresh tokens
- Multiple device sessions using token families
- Account-wide token revocation on password change or global logout
- Five-attempt login lockout for 15 minutes
- Fixed `User` and `Admin` authorization roles
- Transactional email tracking and audit logs
- Cloudinary-backed file storage

## Architecture

```text
API -> Application -> Domain
 |                    ^
 `-> Infrastructure --|
```

See `Rbac-System-Project-Structure.md` for responsibilities, dependency rules,
planned request flow, and persistence conventions.

## Prerequisites

- .NET 8 SDK or a newer compatible SDK
- PostgreSQL
- Git

## Setup

From the repository root:

```powershell
dotnet restore Rbac-System.sln
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=rbac_dev;Username=YOUR_USERNAME;Password=YOUR_PASSWORD" --project src/RbacSystem.API
dotnet build Rbac-System.sln
dotnet run --project src/RbacSystem.API
```

The API project already has a `UserSecretsId`. Each contributor supplies a local
connection string for their own PostgreSQL database. Contributors share committed
migrations and therefore the same schema, but they do not share credentials or
local data.

Do not add connection strings, JWT keys, OAuth secrets, email-provider credentials,
or Cloudinary credentials to tracked `appsettings` files.

In production, provide secrets through environment variables or a managed secret
store. For example, `ConnectionStrings__DefaultConnection` maps to
`ConnectionStrings:DefaultConnection` in ASP.NET Core configuration.

## Checks

```powershell
dotnet build Rbac-System.sln
dotnet test Rbac-System.sln
dotnet format Rbac-System.sln --verify-no-changes
```

## Future migration workflow

After the core entity model is implemented, migrations will be generated from the
repository root:

```powershell
dotnet ef migrations add InitialCreate --project src/RbacSystem.Infrastructure --startup-project src/RbacSystem.API --output-dir Persistence/Migrations
dotnet ef database update --project src/RbacSystem.Infrastructure --startup-project src/RbacSystem.API
```

Migration source files are committed. Local PostgreSQL data, database dumps, secret
values, and the contents of a database's `__EFMigrationsHistory` table are not.

