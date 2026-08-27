# Contributing to Dotnet RBAC System

First off, thank you for considering contributing to the RBAC System! 🎉

We welcome contributions from everyone. Whether you're fixing a bug, adding a feature, or improving documentation, your help is invaluable.

## 📜 Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct. Please read it carefully:

- **Be respectful** — Treat everyone with kindness and professionalism.
- **Be inclusive** — Use inclusive language and welcome diverse perspectives.
- **Be collaborative** — Work together constructively and give constructive feedback.
- **Be responsible** — Take ownership of your work and communicate clearly.

---

## 🚀 Getting Started

1. ### Clone the Repository:
```bash
   git clone https://github.com/Steve-s-Circle-on-System-Design/dotnet-rbac-system.git
   cd dotnet-rbac-system
```

---

2. ### Restore dependencies:
```bash
    dotnet restore
```

---

3. ### Configure Secrets:
    The API project already has a `UserSecretsId`. Configure credentials locally
    from the repository root (not in appsettings.json):

```bash
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=rbac_system;Username=postgres;Password=yourpassword" --project src/RbacSystem.API
```

    #### App Config
    dotnet user-secrets set "Kestrel:Port" "5000" --project src/RbacSystem.API

    #### JWT Configuration Secrets
    dotnet user-secrets set "Jwt:Key" "your_super_secret_access_key_at_least_32_chars" --project src/RbacSystem.API
    dotnet user-secrets set "Jwt:RefreshTokenHashSecret" "another_secret_at_least_32_chars_long" --project src/RbacSystem.API
    dotnet user-secrets set "Jwt:EmailVerificationSecret" "your_email_verification_secret_key" --project src/RbacSystem.API

    `Jwt:Key` and `Jwt:RefreshTokenHashSecret` are both required and must be at
    least 32 characters. The API validates them at startup and refuses to start
    otherwise, rather than failing on the first login.

    #### Non-secret tunables — do NOT put these in user secrets

    Token lifespans and hashing costs are configuration, not literals in code. Their
    defaults are tracked in `appsettings.json` (`Security:PasswordHashing:WorkFactor`
    and the `Auth:*` expiry keys — see the README's "Tunable settings" table). Override
    them per environment with environment variables using `__` as the separator:

```bash
    # e.g. cheaper hashing in CI so test runs stay fast
    export Security__PasswordHashing__WorkFactor=5
    export Auth__AccessTokenExpiryMinutes=15
```

    When adding a feature that needs a lifetime, expiry, cost, or retry count, add a
    key to `appsettings.json` and bind it with the options pattern. Never hardcode the
    value.

    #### Cloudinary Credentials
    dotnet user-secrets set "Cloudinary:CloudName" "your_cloud_name" --project src/RbacSystem.API
    dotnet user-secrets set "Cloudinary:ApiKey" "your_api_key" --project src/RbacSystem.API
    dotnet user-secrets set "Cloudinary:ApiSecret" "your_api_secret" --project src/RbacSystem.API

    Each contributor uses a separate local PostgreSQL database and data while
    committed EF Core migrations keep the schema consistent.

---

4. ### Run Database Migrations:
```bash
    dotnet ef migrations add InitialCreate --project src/RbacSystem.Infrastructure --startup-project src/RbacSystem.API --output-dir Persistence/Migrations
    dotnet ef database update --project src/RbacSystem.Infrastructure --startup-project src/RbacSystem.API
```

    Commit migration source files with schema changes. Synchronize with the target
    branch before generating a migration, review `Up()` and `Down()`, and do not
    edit a migration after it reaches a shared branch or environment.

---

5. ### Fire Up the Server:
    #### Development watch mode
```bash
    dotnet watch run
```

    #### Production build
```bash
    dotnet publish -c Release
```

---

6. ### Create a Branch:
    #### Create a branch for your feature or bugfix:
```bash
    git checkout -b feature/your-feature-name
```
    ### or
```bash
    git checkout -b fix/your-bugfix-name
```

---

7. ### Development Guidelines:
    #### Code Style:
    ✅ Use C# with nullable reference types enabled for all new code

    ✅ Follow standard .NET/C# naming conventions (PascalCase for public members, camelCase for locals and private fields)

    ✅ Use `dotnet format` and an `.editorconfig` for consistent formatting

    ✅ Write meaningful variable and method names

    ✅ Keep methods focused and single-purpose; business logic belongs in Services/, not Controllers/

    ### Run formatter check
```bash
    dotnet format --verify-no-changes
```

    ### Auto-fix formatting issues
```bash
    dotnet format
```

    #### Line endings

    `.editorconfig` requires LF, and `.gitattributes` (`* text=auto eol=lf`) makes
    Git check every text file out as LF on Windows, macOS, and Linux alike. You do
    not need to change `core.autocrlf`.

    If you cloned **before** `.gitattributes` was added, your working copy is still
    CRLF and `dotnet format --verify-no-changes` will report hundreds of `ENDOFLINE`
    errors on files you never touched. Refresh the working copy once:

```bash
    git rm --cached -r .
    git reset --hard
```

    That rewrites the working copy using the new rules. It changes no committed
    content — the repository already stores LF — so `git diff` stays empty
    afterwards. Commit any real work first, since `reset --hard` discards
    uncommitted changes.

---

8. ### Commit Message Convention:
    #### We follow the Conventional Commits specification:
<type>(<scope>): <subject>

<body>

<footer>

#### Types
    ✅ feat — New feature

    ✅ fix — Bug fix

    ✅ docs — Documentation changes

    ✅ style — Code style changes (formatting, whitespace, etc.)

    ✅ refactor — Code refactoring

    ✅ perf — Performance improvements

    ✅ test — Adding or updating tests

    ✅ chore — Build process, dependencies, etc.

    #### Examples

feat(auth): implement Google OAuth 2.0 integration

Adds OAuth flow with automatic account linking for existing users.

Closes #123

fix(email): resolve retry logic in exponential backoff

Fixes issue where retries would not trigger after the first failure.

Fixes #456

## Branch Naming Convention

Please create branches using the format:

feat/ – New feature
fix/ – Bug fix
docs/ – Documentation changes
test/ – Adding or updating tests
refactor/ – Code restructuring without changing behavior
chore/ – Project setup, dependencies, or maintenance
perf/ – Performance improvements
style/ – Formatting, whitespace, or code style changes

  #### Examples
 feat/user-registration
feat/jwt-authentication
feat/role-permission-management

fix/login-validation-error
fix/password-reset-token

docs/update-readme
docs/add-api-contract

test/auth-service-tests
test/user-controller-tests

refactor/cleanup-auth-service

chore/add-github-actions
  
---

9. ### Testing Requirements:
    ✅ All new features must include unit tests (xUnit)

    ✅ Bug fixes should include regression tests

    ✅ Maintain or improve code coverage (target: 80%+)

    ✅ Tests should be isolated and independent

    ### Run all tests
```bash
    dotnet test
```

    #### Database-backed tests

    Some behaviour, such as the atomic failed-login counter, can only be proven
    against a real PostgreSQL. Those tests skip themselves unless a test database is
    configured, so `dotnet test` stays runnable without one:

```bash
    docker run -d --name rbac-test-db -e POSTGRES_PASSWORD=postgres       -e POSTGRES_DB=rbac_test -p 55433:5432 postgres:18
    export ConnectionStrings__TestDatabase="Host=localhost;Port=55433;Database=rbac_test;Username=postgres;Password=postgres"
    dotnet test
```

    CI always provides this, so the tests genuinely run on every pull request. If
    `dotnet test` reports skipped tests locally, that is why.

    ### Run tests with coverage
```bash
    dotnet test --collect:"XPlat Code Coverage"
```

---

10. ### Documentation Standards:
    ✅ Update README.md for user-facing changes

    ✅ Update CONTRIBUTORS.md for developer-facing changes

    ✅ Add XML doc comments (`///`) to new public methods and classes

    ✅ Include Swagger/OpenAPI annotations for all endpoints

    ✅ Create separate markdown files in /docs for complex features

---

11. ### Security Best Practices:
    ✅ Never commit sensitive data (passwords, secrets, API keys)

    ✅ Always validate and sanitize user inputs

    ✅ Always use parameterized queries / EF Core LINQ (never raw string-concatenated SQL)

    ✅ Always use `dotnet user-secrets` locally and environment variables/secret managers in production

    ✅ Always run a vulnerability scan before PR submission

    ### Check for vulnerable packages
```bash
    dotnet list package --vulnerable
```

    ### Check for outdated packages
```bash
    dotnet list package --outdated
```

---

12. ### Pull Request Process:
    #### Before Submitting a PR:
    ✅ Update your fork: Rebase against the latest main branch

    ✅ Run tests: Ensure all tests pass locally

    ✅ Update documentation: If you added/changed features, update README and docs

    ✅ Check coverage: Ensure coverage doesn't decrease

    ✅ Self-review: Review your own code before submission

    #### PR Submission Checklist:
    ✅ I have read and followed the code of conduct

    ✅ My code follows the project's style guide

    ✅ I have added/updated tests that prove my fix/feature works

    ✅ I have updated the documentation accordingly

    ✅ My commit messages follow the conventional commits spec

    ✅ I have linked related issues in the PR description

    ✅ All new and existing tests pass

    #### PR Title Convention:

<type>(<scope>): <description>

#### PR Description Template:

## Description
[Provide a clear and concise description of what this PR does]

#### Related Issues
[Link any related issues using #issue-number]

#### Type of Change
- [ ] Bug fix (non-breaking change)
- [ ] New feature (non-breaking change)
- [ ] Breaking change
- [ ] Documentation update

#### Testing
- [ ] Unit tests added/updated
- [ ] Manually tested

#### Screenshots (if applicable)
[Add screenshots to demonstrate the changes]

## Checklist
- [ ] Code follows project style
- [ ] Documentation has been updated
- [ ] Tests pass locally
- [ ] No new warnings/errors
- [ ] Security best practices followed

---

13. ### Issue Guidelines
    #### Reporting Bugs

    Use the Bug Report template and include:

    ✅ Title: Clear, descriptive summary

    ✅ Environment: OS, .NET SDK version

    ✅ Steps to Reproduce: Step-by-step guide

    ✅ Expected Behavior: What should happen

    ✅ Actual Behavior: What actually happens

    ✅ Screenshots: If applicable

    ✅ Logs/Errors: Full error messages and stack traces

    #### Suggesting Features

    Use the Feature Request template and include:

    ✅ Title: Clear, descriptive summary

    ✅ Problem: What problem does this solve?

    ✅ Solution: How would this feature work?

    ✅ Alternatives: Are there workarounds?

    ✅ Priority: How important is this to you?

    #### Labels

    Our maintainers will add labels to help categorize issues:

    ✅ bug — Something isn't working

    ✅ feature — New feature request

    ✅ enhancement — Improvement to existing feature

    ✅ documentation — Docs changes needed

    ✅ good-first-issue — Good for newcomers

    ✅ help-wanted — Community contributions welcome

---

14. ### Code Review Process
    ✅ Initial Review — Maintainers will review within 48 hours

    ✅ Feedback — Address comments and push updates

    ✅ Approval — At least one maintainer approval required

    ✅ Merge — PR will be merged once all checks pass

    #### Review Etiquette

    ✅ Be constructive and specific in feedback

    ✅ Explain the "why" behind suggestions

    ✅ Ask clarifying questions when needed

    ✅ Be open to different approaches

    ✅ Respond to reviews promptly

---

15. ### Security Vulnerabilities

    If you discover a security vulnerability, please do NOT open a public issue.

    Instead, email us directly at security@rbac-system.com with:

    ✅ A detailed description of the vulnerability

    ✅ Steps to reproduce

    ✅ Potential impact

    ✅ Suggested mitigation

    We will respond within 48 hours and work with you to resolve the issue.

---

16. ### Recognition

    Contributors who make significant contributions will be:

    ✅ Added to the CONTRIBUTORS.md file

    ✅ Recognized in release notes

    ✅ Considered for maintainer roles (after consistent contributions)

---

Thank you for contributing to RBAC System!
