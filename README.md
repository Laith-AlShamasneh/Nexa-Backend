# Nexa

A multi-tenant SaaS platform for education institutes, training centers, and language academies — built to expand into other service-business verticals (clinics, beauty centers, ...) later without reworking the core. Full product context: [PRODUCT_CONTEXT.md](PRODUCT_CONTEXT.md).

## Current development status

**Phase 0 (solution cleanup and foundation) is complete.** The solution builds cleanly and `WebApi` starts and serves `/health` + `/openapi/v1.json`. There are **no real business API endpoints yet** — see [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md) for what's next (Phase 1: Platform Foundation, then Phase 2: Tenant Onboarding).

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

Full schema design and rationale: [docs/database/DATABASE_FINAL_BLUEPRINT.md](docs/database/DATABASE_FINAL_BLUEPRINT.md).

## Documentation index

- [docs/SOLUTION_STRUCTURE.md](docs/SOLUTION_STRUCTURE.md) — project responsibilities, dependency direction, folder conventions
- [docs/ARCHITECTURE_RULES.md](docs/ARCHITECTURE_RULES.md) — non-negotiable architecture rules
- [docs/DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md) — how to add a new feature, completion checklist
- [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md) — naming, nullability, DI, logging, validation conventions
- [docs/MULTI_TENANCY.md](docs/MULTI_TENANCY.md) — tenant isolation model in full
- [docs/SECURITY_BASELINE.md](docs/SECURITY_BASELINE.md) — implemented vs. planned security posture
- [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md) — phased implementation plan
- [docs/database/DATABASE_FINAL_BLUEPRINT.md](docs/database/DATABASE_FINAL_BLUEPRINT.md) — full database design
- [PRODUCT_CONTEXT.md](PRODUCT_CONTEXT.md) — product vision and business context

## Current limitations

- No real API endpoints exist yet (only `/health` and `/openapi/v1.json`).
- No stored procedures exist yet for Authentication, Notifications, Onboarding, or BackgroundJobs — the C# repositories reference SP names that must still be written.
- `IUserContext.UserId` and the Authentication DB models use `long` IDs, while the finalized database design uses `Guid` (`UNIQUEIDENTIFIER`) for `Users.Id` — this mismatch needs reconciling before the Identity phase ships (see the Phase 0 cleanup report / remaining risks).
- Row-Level Security (the planned third layer of tenant-isolation defense) is not yet implemented.
- No automated tests exist yet.

## Next implementation phase

**Phase 1 — Platform Foundation**, followed by **Phase 2 — Tenant Onboarding** (see [docs/PROJECT_ROADMAP.md](docs/PROJECT_ROADMAP.md)). Do not begin CRM/Billing/Attendance feature work before these land.
