# RBAC Auth System (C# / .NET)

Role-Based Access Control (RBAC) system built with ASP.NET Core and Entity Framework Core.

This is the C#/.NET implementation of the team's multi-language RBAC project. Sibling implementations exist in Python (FastAPI), Go, and TypeScript. See the API Contract section below for how this service is expected to line up with those.

> **Current branch status:** the solution contains the Clean Architecture and
> PostgreSQL/EF Core foundation. The seven-table domain model, entity mappings,
> and initial migration are implemented. Authentication use cases, repositories,
> and endpoints will be added with their related features.

## Key Deliverables

| Feature        | Description |
|----------------|--------------|
| Authentication | Complete auth flow: register, login, email verification, password reset |
| Security | JWT token rotation, account lockout, RBAC, and endpoint defense |
| Email System | Event-driven email delivery with transactional tracking and analytics |
| File Upload | Modular file upload system with Cloudinary integration |
| Admin Dashboard | User management, email analytics, and system monitoring (optional) |

## Tech Stack

- Runtime: .NET 8
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: PostgreSQL
- Auth: JWT Bearer tokens (access + rotating refresh tokens), ASP.NET Core Identity, Google OAuth 2.0
- Password hashing: bcrypt (BCrypt.Net-Next)
- Event handling: likely MediatR for decoupled domain events, or BackgroundService for async processing (open decision)
- Email delivery: transactional provider (e.g. SendGrid, Postmark) vs raw SMTP (open decision)
- File storage: Cloudinary (CloudinaryDotNet)
- Docs: Swagger / OpenAPI

## Core Features

### Authentication

Three ways in: email + password with bcrypt hashing, Google OAuth 2.0 with automatic account linking, and passwordless login via a 6-digit magic OTP sent by email.

### Token Lifecycle

Short-lived access tokens (15 minute expiry) for normal requests, single-use refresh tokens (7 day expiry) with rotation for sessions, and automatic revocation of tokens on password change or logout.

### Account Lockout

Failed login attempts are tracked in real time. Five consecutive failures locks the account for 15 minutes, calculated via timestamp math, with a security alert emailed to the user when a lock occurs.

### Role-Based Access Control

Two tiers to start: User for standard authenticated access, Admin for privileged operations. Enforced via custom `[Roles]` attributes and policy-based or guard-style authorization on protected routes.

### Endpoint Defense

Security headers to prevent XSS and clickjacking, CORS with dynamic origin validation, and strict input validation/sanitization on all incoming requests.

### Event-Driven Email System

Emails are sent asynchronously so users never wait on them during a request. Every outbound email is logged in an `email_logs` table for full visibility into what was sent, to whom, and when. Delivery status moves through a state machine: Pending, Sent, Delivered, Opened, Clicked. Outbound calls to the email provider are wrapped with retry logic (exponential backoff) for resilience, and aggregated metrics are tracked for open rate, bounce rate, click rate, and delivery rate.

### File Upload

An isolated, swappable file upload module with no vendor lock-in baked into the rest of the app. Files stream directly from memory (no disk writes) straight to Cloudinary.

## Project Structure

```
dotnet-rbac-system/
├── Rbac-System.sln
├── src/
│   ├── RbacSystem.Domain/          # Entities, value objects — no external dependencies
│   ├── RbacSystem.Application/     # Interfaces, use-cases — depends on Domain only
│   ├── RbacSystem.Infrastructure/  # EF Core, PostgreSQL, repositories — depends on Application + Domain
│   └── RbacSystem.API/             # Controllers, Swagger, Program.cs — depends on Application + Infrastructure
└── Tests/
    └── RbacSystem.Tests/           # xUnit tests — references all src layers
```

Dependency direction: `API → Infrastructure → Application → Domain` (Domain has zero outbound dependencies).

## Getting Started

### Prerequisites

- .NET 8 SDK (https://dotnet.microsoft.com/download)
- PostgreSQL (running locally or accessible via connection string)
- A Cloudinary account (for file upload)
- Credentials for the chosen email provider
- A Google Cloud OAuth 2.0 client ID/secret (for social login)
- Git

### Setup

Clone the repo:

```bash
git clone https://github.com/Steve-s-Circle-on-System-Design/dotnet-rbac-system.git
cd dotnet-rbac-system
dotnet restore Rbac-System.sln
```

### Configuration

Do not commit real credentials. The API project already has a `UserSecretsId`.
Configure local values from the repository root with:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=rbac_system;Username=postgres;Password=yourpassword" --project src/RbacSystem.API
dotnet user-secrets set "Jwt:Key" "your-local-dev-secret-at-least-32-chars" --project src/RbacSystem.API
dotnet user-secrets set "Jwt:RefreshTokenHashSecret" "another-local-dev-secret-at-least-32-chars" --project src/RbacSystem.API
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name" --project src/RbacSystem.API
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key" --project src/RbacSystem.API
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret" --project src/RbacSystem.API
dotnet user-secrets set "Google:ClientId" "your-google-client-id" --project src/RbacSystem.API
dotnet user-secrets set "Google:ClientSecret" "your-google-client-secret" --project src/RbacSystem.API
```

Each contributor uses a separate local database and data. EF Core migration files
are committed so every contributor and environment shares the same schema; local
credentials, database dumps, and database data are not committed.

#### Tunable settings

Security costs and token lifespans are configuration, never hardcoded constants, so
they can be tuned per environment without a code change. Non-secret defaults live in
`appsettings.json`; override any of them per environment with an environment
variable, using `__` as the section separator (for example
`Security__PasswordHashing__WorkFactor=13`).

| Setting | Default | Purpose |
|---|---|---|
| `Security:PasswordHashing:WorkFactor` | `12` | BCrypt cost. Each increment doubles hashing time. Valid range 4–31. Lower it in CI to keep test runs fast; raise it as hardware improves. |
| `Auth:AccessTokenExpiryMinutes` | `15` | Access-token lifespan. |
| `Auth:RefreshTokenExpiryDays` | `7` | Refresh-token lifespan. |
| `Auth:EmailVerificationTokenExpiryHours` | `24` | Email-verification token lifespan. |
| `Auth:OtpExpiryMinutes` | `10` | Magic-login OTP lifespan. |
| `Auth:Lockout:MaxFailedAttempts` | `5` | Consecutive failed sign-ins that lock an account. |
| `Auth:Lockout:DurationMinutes` | `15` | How long an account stays locked once the threshold is reached. |
| `Jwt:Issuer` | `RbacSystem` | Issuer stamped on, and validated in, access tokens. |
| `Jwt:Audience` | `RbacSystemUsers` | Audience stamped on, and validated in, access tokens. |

`Security:PasswordHashing:WorkFactor`, `Auth:AccessTokenExpiryMinutes`,
`Auth:RefreshTokenExpiryDays`, the `Auth:Lockout:*` pair, `Jwt:Issuer` and `Jwt:Audience` are read today. The
remaining `Auth:*` lifespans are the agreed keys for the features that consume them.

Two settings are **secrets** and must never appear in `appsettings.json`. Both are
required, and the API refuses to start without them:

| Secret | Purpose |
|---|---|
| `Jwt:Key` | Signs and validates access tokens. Minimum 32 characters — HMAC-SHA256 rejects anything shorter. |
| `Jwt:RefreshTokenHashSecret` | Keys the HMAC applied to refresh tokens before storage, so a leaked database cannot be matched against intercepted tokens. Minimum 32 characters. |

Only `Security:PasswordHashing:WorkFactor` is read today, by the registration
feature. The `Auth:*` lifespans are the agreed keys for the token-issuing features
and are consumed as those land — they are listed here so that no lifespan is ever
written as a literal in code.

Secrets — connection strings, JWT signing keys, OAuth client secrets, and email or
Cloudinary credentials — never belong in `appsettings.json`. Use `dotnet user-secrets`
locally and environment variables or a managed secret store in production.

### Build

```bash
dotnet build Rbac-System.sln
```

### Test

```bash
dotnet test Rbac-System.sln
```

### Format

```bash
# Check formatting without making changes (used in CI)
dotnet format Rbac-System.sln --verify-no-changes

# Apply formatting
dotnet format Rbac-System.sln
```

### Run

```bash
dotnet run --project src/RbacSystem.API
```

Swagger UI loads at `http://localhost:<port>/swagger` in Development mode, which is
the URL `launchSettings.json` opens on startup. Use the **Authorize** button with the
`accessToken` returned by `/api/auth/login` to call protected endpoints.

### Apply EF Core Migrations

```bash
# Add a migration (run from repo root)
dotnet ef migrations add InitialCreate \
  --project src/RbacSystem.Infrastructure \
  --startup-project src/RbacSystem.API \
  --output-dir Persistence/Migrations

# Apply to the database
dotnet ef database update \
  --project src/RbacSystem.Infrastructure \
  --startup-project src/RbacSystem.API
```

Synchronize with the target branch before generating a migration. Do not edit a
migration after it has reached a shared branch; create a corrective migration.

## API Contract

### `POST /api/auth/register`

Creates an unverified account with the default `User` role. The request shape matches
the TypeScript and Python implementations so all four services stay interchangeable.

Request:

```json
{ "email": "ada@example.com", "password": "Str0ng!Passw0rd" }
```

The email is trimmed and lowercased before storage and compared case-insensitively.
The display name is derived from the email's local part, as the other
implementations do, because the shared contract carries no name field.

Password policy: at least 8 characters, at most 72 UTF-8 bytes (BCrypt ignores input
beyond that), and at least one lowercase letter, one uppercase letter, and one
special character.

| Status | Body | When |
|---|---|---|
| `201` | `{ "message": "Sign Up successful, verify Email." }` | Account created |
| `400` | `ProblemDetails` with detail `Email is already registered` | Address already in use |
| `400` | `ValidationProblemDetails` | Email malformed or password fails the policy |

### `POST /api/auth/login`

Authenticates a credential pair and starts a session. Request:

```json
{ "email": "ada@example.com", "password": "Str0ng!Passw0rd" }
```

Only presence and email format are validated. The registration password policy is
deliberately not applied, so a wrong password returns `401` rather than a `400` that
would disclose the policy.

| Status | Body | When |
|---|---|---|
| `200` | `{ "accessToken", "refreshToken", "tokenType": "Bearer", "expiresIn": 900 }` | Authenticated |
| `401` | `ProblemDetails`, detail `Invalid email or password` | Unknown email **or** wrong password — identical for both |
| `403` | detail `Please verify your email to continue` | Email not verified |
| `403` | detail `Account locked due to multiple failed attempts. Try again later.` | Lockout active |

Five consecutive incorrect passwords lock an account for 15 minutes, both values
configurable above. The attempt that trips the lock still returns `401`, since the
password genuinely was wrong; the `403` begins on the next attempt. Attempts made
during a lockout are not counted and do not extend it, and a lockout that has
expired starts a fresh sequence rather than resuming from the count that caused it.
A successful sign-in clears the counter.
| `400` | `ValidationProblemDetails` | Email missing or malformed |

The access token is a JWT carrying `sub`, `email`, `role`, `sid`, `jti` and
`token_version`, expiring after `Auth:AccessTokenExpiryMinutes`. `sid` is the token
family — the session identifier that refresh-token rotation will rotate within.

The refresh token is 48 bytes of cryptographic randomness, opaque to clients, valid
for `Auth:RefreshTokenExpiryDays`. Only its HMAC is stored; the raw value is never
persisted or logged.

### Remaining endpoints

The rest of the endpoints, request/response shapes, and status codes are still being
finalized with the other language teams. This section should eventually also cover:

- Auth endpoints (register, login, Google OAuth callback, magic OTP request/verify, email verification, password reset, refresh token)
- User and Admin role-protected endpoints
- File upload endpoints
- Admin dashboard endpoints (if built)
- Expected response formats and error shapes

## Open Decisions

Things not locked in yet, tracked here so nobody assumes they're settled:

- Event handling mechanism (MediatR vs BackgroundService vs something else)
- Email delivery approach (transactional provider like SendGrid/Postmark vs raw SMTP with custom tracking)
- Authorization policies and the exact Admin management endpoint contract
- Whether the Admin Dashboard is being built for this milestone or deferred

## Branching & Workflow

See CONTRIBUTORS.md for branch naming, commit conventions, and PR process.

## License
