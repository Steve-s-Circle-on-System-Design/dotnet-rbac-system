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
   git clone https://github.com/<org>/dotnet-rbac-system.git
   cd dotnet-rbac-system
```

---

2. ### Restore dependencies:
```bash
    dotnet restore
```

---

3. ### Configure Secrets:
    Initialize user secrets and configure your credentials locally (never in appsettings.json):

```bash
    dotnet user-secrets init
```

    #### App Config
    dotnet user-secrets set "Kestrel:Port" "5000"

    #### JWT Configuration Secrets
    dotnet user-secrets set "Jwt:Key" "your_super_secret_access_key"
    dotnet user-secrets set "Jwt:AccessTokenExpiryMinutes" "15"
    dotnet user-secrets set "Jwt:EmailVerificationSecret" "your_email_verification_secret_key"

    #### Cloudinary Credentials
    dotnet user-secrets set "Cloudinary:CloudName" "your_cloud_name"
    dotnet user-secrets set "Cloudinary:ApiKey" "your_api_key"
    dotnet user-secrets set "Cloudinary:ApiSecret" "your_api_secret"

    #### Database
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=rbac_system;Username=postgres;Password=yourpassword"

---

4. ### Run Database Migrations:
```bash
    dotnet ef migrations add InitialCreate
    dotnet ef database update
```

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