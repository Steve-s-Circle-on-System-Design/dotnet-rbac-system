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
└── tests/
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
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name" --project src/RbacSystem.API
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key" --project src/RbacSystem.API
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret" --project src/RbacSystem.API
dotnet user-secrets set "Google:ClientId" "your-google-client-id" --project src/RbacSystem.API
dotnet user-secrets set "Google:ClientSecret" "your-google-client-secret" --project src/RbacSystem.API
```

Each contributor uses a separate local database and data. EF Core migration files
are committed so every contributor and environment shares the same schema; local
credentials, database dumps, and database data are not committed.

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

Swagger UI loads at `http://localhost:<port>` (root URL) in Development mode.

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

Endpoints, request/response shapes, and status codes are still being finalized with the other language teams so that all four implementations expose a consistent contract. This section will be filled in once that's agreed, and should eventually cover:

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
