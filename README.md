# RBAC Auth System (C# / .NET)

Role-Based Access Control (RBAC) system built with ASP.NET Core and Entity Framework Core.

This is the C#/.NET implementation of the team's multi-language RBAC project. Sibling implementations exist in Python (FastAPI), Go, and TypeScript. See the API Contract section below for how this service is expected to line up with those.

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

dotnet-rbac-system/
- Controllers/   API endpoints
- Models/        Domain entities (User, Role, Permission, EmailLog, etc.)
- DTOs/          Request/response shapes exposed to clients
- Services/      Business logic (auth, role assignment, permission checks, email, file upload)
- Data/          DbContext, migrations, seed data
- Tests/
- Program.cs     App entry point and middleware pipeline
- appsettings.json   Configuration (do not commit real secrets here)

Note: given the scope here, Services/ will likely need subfolders (Auth/, Email/, FileUpload/) as this grows. Open to discussion once implementation starts.

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

git clone https://github.com/<org>/dotnet-rbac-system.git
cd dotnet-rbac-system
dotnet restore

### Configuration

Do not put real credentials or secrets in appsettings.json since that file gets committed to the repo. Use dotnet user-secrets instead for local development:

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=rbac_system;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Jwt:Key" "your-local-dev-secret-here"
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
dotnet user-secrets set "Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Google:ClientSecret" "your-google-client-secret"

### Run

dotnet run

Swagger UI will be available in the browser once the project is running in development mode, at a URL shown in the terminal output (something like https://localhost:xxxx/swagger).

### Apply migrations

Once the DbContext and initial models are in place:

dotnet ef migrations add InitialCreate
dotnet ef database update

## API Contract

Endpoints, request/response shapes, and status codes are still being finalized with the other language teams so that all four implementations expose a consistent contract. This section will be filled in once that's agreed, and should eventually cover:

- Auth endpoints (register, login, Google OAuth callback, magic OTP request/verify, email verification, password reset, refresh token)
- Role and permission management endpoints
- User-role assignment endpoints
- File upload endpoints
- Admin dashboard endpoints (if built)
- Expected response formats and error shapes

## Open Decisions

Things not locked in yet, tracked here so nobody assumes they're settled:

- Event handling mechanism (MediatR vs BackgroundService vs something else)
- Email delivery approach (transactional provider like SendGrid/Postmark vs raw SMTP with custom tracking)
- Whether roles/permissions are seeded or admin-managed at runtime
- Whether the Admin Dashboard is being built for this milestone or deferred

## Branching & Workflow

See CONTRIBUTORS.md for branch naming, commit conventions, and PR process.

## License

See LICENSE.