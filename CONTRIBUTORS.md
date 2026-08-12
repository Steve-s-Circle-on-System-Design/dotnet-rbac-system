# Contributing to the .NET RBAC System

Thank you for contributing. Keep changes focused, communicate overlapping work,
and treat security and schema compatibility as shared responsibilities.

## Getting started

```powershell
git clone https://github.com/<org>/dotnet-rbac-system.git
cd dotnet-rbac-system
dotnet restore Rbac-System.sln
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=rbac_dev;Username=YOUR_USERNAME;Password=YOUR_PASSWORD" --project src/RbacSystem.API
dotnet build Rbac-System.sln
dotnet test Rbac-System.sln
```

The API project already has a `UserSecretsId`; contributors should not run
`dotnet user-secrets init` again. Each contributor uses their own local PostgreSQL
database and data while sharing the committed EF Core schema migrations.

## Secrets and local data

- Never commit connection strings, passwords, JWT signing keys, OAuth secrets,
  email-provider credentials, Cloudinary credentials, or database dumps.
- Store local secrets against `src/RbacSystem.API` with .NET user secrets.
- Use environment variables or a managed secret store in deployed environments.
- Do not log resolved secret values.

## Database migrations

- Commit migrations with every schema-changing feature.
- Synchronize with the target branch before generating a migration.
- Coordinate with contributors changing the same entities or tables.
- Commit the migration, designer file, and model snapshot together.
- Review and test both `Up()` and `Down()` on a disposable PostgreSQL database.
- Do not edit or delete a migration after it reaches a shared branch or environment.
  Create a corrective migration instead.

## Code quality

Before opening a pull request, run:

```powershell
dotnet build Rbac-System.sln
dotnet test Rbac-System.sln
dotnet format Rbac-System.sln --verify-no-changes
```

The repository `.editorconfig` is the formatting and style agreement. Avoid
unrelated formatting changes in feature pull requests.

## Pull requests

- Use a focused branch and descriptive commits.
- Explain the reason for the change and how it was verified.
- Include committed migrations when the schema changes.
- Update relevant documentation and tests.
- Keep controllers thin, business rules in Domain/Application, and persistence
  details in Infrastructure.

## Conduct

Be respectful, inclusive, collaborative, and responsible. Review ideas and code
constructively, and never expose another contributor's credentials or local data.

