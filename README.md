# Nexa

A multi-tenant SaaS platform for education institutes, training centers, and language academies — built to expand into other service-business verticals (clinics, beauty centers, ...) later without reworking the core. Full product context: [PRODUCT_CONTEXT.md](PRODUCT_CONTEXT.md).

## Current development status

**Phase 0 (solution cleanup and foundation) is complete.** Tenant Onboarding (`POST /api/organizations/register`) and Email Confirmation (`POST /api/auth/confirm-email`, `POST /api/auth/resend-email-confirmation` — see [docs/EMAIL_CONFIRMATION.md](docs/EMAIL_CONFIRMATION.md)) are both implemented end-to-end and verified live, including a genuine concurrent-request race test for confirm-email. `WebApi` starts and serves `/health` and interactive API docs at `/swagger` (Development only). See [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md) for what's next — **Login is the recommended next module.**

A substantial amount of generic authentication/notification/background-job/onboarding infrastructure was inherited from an earlier project and kept because it's genuinely reusable — but none of it is yet connected to a real database (no stored procedures exist for it) or a real HTTP endpoint. See [docs/SECURITY_BASELINE.md](docs/SECURITY_BASELINE.md) for a precise "implemented vs. planned" breakdown before relying on anything described here.

## Technology stack

- ASP.NET Core (.NET 10) — Web API
- SQL Server — Dapper + stored procedures (no ORM)
- Practical Clean Architecture (Domain / Application / Infrastructure / Shared / WebApi) — no MediatR, no CQRS pipeline, no Vertical Slice Architecture
- FluentValidation for request validation
- Multi-tenant, shared-database/shared-schema, `OrganizationId`-isolated (see [docs/MULTI_TENANCY.md](docs/MULTI_TENANCY.md))

## Solution projects

| Project | Responsibility |
|---|---|
| `Domain` | Entities, value objects, domain exceptions, business invariants. No dependencies. |
| `Application` | Use-case services, DTOs, validators, repository/service contracts. Depends on `Domain` + `Shared`. |
| `Infrastructure` | Dapper/SQL Server, JWT, password hashing, email, storage, caching, background jobs — implements `Application`'s contracts. |
| `Shared` | Small cross-cutting primitives: Result types, enums, message-key constants. |
| `WebApi` | Composition root — `Program.cs`, DI wiring, middleware, OpenAPI, health checks. |

Full breakdown, dependency diagram, and folder conventions: [docs/SOLUTION_STRUCTURE.md](docs/SOLUTION_STRUCTURE.md).

## How to build

```bash
dotnet restore
dotnet build
```

Requires the .NET 10 SDK.

## How to run

```bash
dotnet run --project WebApi/WebApi.csproj
```

The API starts even without a configured database — background jobs that need the DB will log errors and keep retrying, but the process stays up and `/health` responds. Once a real connection string is set, most of the wired-up services (Auth, Notifications, Onboarding, BackgroundJobs) will still need their stored procedures written before they function end-to-end (see [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md)).

## Configuration requirements

Set the following via user-secrets (local dev) or environment-specific configuration (never commit real values — `appsettings.json` ships only empty placeholders):

- `ConnectionStrings:SqlConnection`
- `Jwt:SecretKey`
- `Smtp:Host` / `Smtp:Username` / `Smtp:Password`

See `WebApi/appsettings.json` for the full list of bound configuration sections (`Jwt`, `Authentication`, `Smtp`, `Storage`, `BackgroundJobs`).

## Database migration order

Numbered SQL scripts under [`Database/Migrations/`](Database/Migrations), applied in order:

1. `001_CreateSchemas.sql` — `tenant`, `identity`, `crm`, `audit`, `billing`, `notification`, `education` schemas
2. `002_Tenant_Organizations_Branches.sql`
3. `003_Identity_Persons_Users.sql`
4. `004_Identity_Roles_Permissions.sql`
5. `005_Identity_Tokens_SignInLogs.sql`
6. `006_Crm_Customers_CustomerNotes.sql`
7. `007_Audit_AuditLogs.sql`
8. `008_Seed_GlobalData.sql` — global permission catalog + system role templates
9. `009_Harden_MultiTenant_Identity.sql` — composite tenant-safe foreign keys, `OrganizationSettings`, `UserSessions`, `UserInvitations`, tenant-local role materialization
10. `010_Bilingual_Name_Fields.sql` — Arabic counterpart columns for every English name field (Organizations, Branches, Persons, Roles, Permissions, Customers)
11. `011_Tenant_Onboarding.sql` — `tenant.usp_Organization_Register`, the single-transaction "register a new organization" procedure
12. `012_BackgroundJobs_And_Scheduling.sql` — `dbo.BackgroundJobs` (work queue) and `dbo.ScheduledJobs` (recurring-job definitions) tables and stored procedures. See [docs/BACKGROUND_JOBS.md](docs/BACKGROUND_JOBS.md).
13. `013_Email_Confirmation.sql` — `identity.usp_EmailConfirmation_Confirm`/`_Resend` (no schema changes needed — migrations 003/005/009 already had every column). See [docs/EMAIL_CONFIRMATION.md](docs/EMAIL_CONFIRMATION.md).

All 13 migrations have been applied to the `Nexa` database on `localhost\SQLEXPRESS`. Full schema design and rationale: [docs/database/DATABASE_FINAL_BLUEPRINT.md](docs/database/DATABASE_FINAL_BLUEPRINT.md); Domain-layer mapping: [docs/DOMAIN_MODEL.md](docs/DOMAIN_MODEL.md).

## Documentation index

- [docs/SOLUTION_STRUCTURE.md](docs/SOLUTION_STRUCTURE.md) — project responsibilities, dependency direction, folder conventions
- [docs/ARCHITECTURE_RULES.md](docs/ARCHITECTURE_RULES.md) — non-negotiable architecture rules
- [docs/DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md) — how to add a new feature, completion checklist
- [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md) — naming, nullability, DI, logging, validation conventions
- [docs/MULTI_TENANCY.md](docs/MULTI_TENANCY.md) — tenant isolation model in full
- [docs/SECURITY_BASELINE.md](docs/SECURITY_BASELINE.md) — implemented vs. planned security posture
- [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md) — phased implementation plan
- [docs/TENANT_ONBOARDING.md](docs/TENANT_ONBOARDING.md) — the organization-registration workflow, end to end
- [docs/EMAIL_CONFIRMATION.md](docs/EMAIL_CONFIRMATION.md) — confirm/resend email-confirmation workflow, end to end
- [docs/BACKGROUND_JOBS.md](docs/BACKGROUND_JOBS.md) — background-job queue and scheduled-job (recurring) database design
- [docs/EMAIL_TEMPLATES.md](docs/EMAIL_TEMPLATES.md) — reusable email template system (base layout, design system, RTL/LTR, how to add a template)
- [docs/DOMAIN_MODEL.md](docs/DOMAIN_MODEL.md) — entity classification, tenant ownership, Dapper materialization strategy
- [docs/database/DATABASE_FINAL_BLUEPRINT.md](docs/database/DATABASE_FINAL_BLUEPRINT.md) — full database design
- [PRODUCT_CONTEXT.md](PRODUCT_CONTEXT.md) — product vision and business context

## Current limitations

- Three real business endpoints exist so far: `POST /api/organizations/register` (Tenant Onboarding), `POST /api/auth/confirm-email`, `POST /api/auth/resend-email-confirmation` (Email Confirmation — see [docs/EMAIL_CONFIRMATION.md](docs/EMAIL_CONFIRMATION.md)). Everything else is `/health` and `/swagger`.
- No stored procedures exist yet for Login/Refresh/Logout/Forgot-Password/Reset-Password or Notifications — the C# repositories reference SP names that must still be written (`AuthService.RegisterAsync` in particular calls `identity.usp_Authentication_SaveConfirmationToken`, which doesn't exist — a separate, currently-unwired flow out of scope for the Email Confirmation module; see [docs/EMAIL_CONFIRMATION.md](docs/EMAIL_CONFIRMATION.md) "Relationship to AuthService"). BackgroundJobs, ScheduledJobs, and Email Confirmation are fully wired end-to-end (database, Application, Infrastructure, WebApi) and verified live.
- The email template system (`WebApi/EmailTemplates/`) only has one fully-built template so far — **Email Confirmation**. The other six job handlers that already call `IEmailTemplateService.RenderAsync` (`WelcomeEmail`, `PasswordResetEmail`, `PasswordChangedEmail`, `EmailChangeRequested`, `EmailChanged`, `OrganizationInvitationEmail`) will still throw `FileNotFoundException` at runtime until their own template files are authored — the shared base layout and file convention are ready for them; see [docs/EMAIL_TEMPLATES.md](docs/EMAIL_TEMPLATES.md) "How to add a new template". `Smtp:*` is also still unconfigured (empty placeholders), so no real email actually sends yet regardless.
- `IUserContext.UserId` and the (unrelated, out-of-scope) Login/Register Authentication DB models use `long` IDs, while the finalized database design uses `Guid` (`UNIQUEIDENTIFIER`) for `Users.Id` — this mismatch needs reconciling before the Login phase ships. Email Confirmation is unaffected — it's built entirely against the real `Guid`-keyed schema.
- Row-Level Security (the planned third layer of tenant-isolation defense) is not yet implemented.
- 23 unit tests exist (`Tests/Application.UnitTests`, xUnit + Moq) covering Tenant Onboarding and Email Confirmation's Application-layer logic. No dedicated SQL-Server-backed integration test project exists yet — verification of stored-procedure behavior (including concurrency) has so far been done via direct `sqlcmd` execution against a real database, documented in each feature's own doc.

## Next implementation phase

**Login and Sign-In Security** (Phase 3 — see [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md)) — the next piece a confirmed user needs. Do not begin CRM/Billing/Attendance feature work before Identity/Authentication lands.
