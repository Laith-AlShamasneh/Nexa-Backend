# Nexa — Development Workflow

## Adding a new feature: the expected sequence

1. **Understand the business use case.** Reread the relevant section of [PRODUCT_CONTEXT.md](../PRODUCT_CONTEXT.md) and, if it touches the database, [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md). Confirm which vertical (generic Core vs. Education-specific) the feature belongs to.
2. **Define the domain behavior.** If the feature introduces a new entity or business rule, add it to `Domain` — entity shape, invariants, domain exceptions. Skip this step if the feature is pure orchestration over existing entities.
3. **Define Application contracts.** Add the feature slice under `Application/Features/<Name>/`: DTOs, the repository interface(s) under `Application/Interfaces/Repositories/`, the service interface under `Application/Interfaces/Services/` (if the feature needs one beyond its own service class), and the service implementation.
4. **Implement Infrastructure persistence/integration.** Add the repository implementation under `Infrastructure/Services/<Name>/`, backed by a stored procedure (add the SP to a new `Database/Migrations/0XX_*.sql` file — never edit an already-applied migration). Register the new interface→implementation binding in `Infrastructure/Extensions/ServiceCollectionExtensions.cs`.
5. **Expose the API endpoint.** Add the endpoint/controller in `WebApi`. It validates input, calls the Application service, maps the result to an HTTP response — nothing else.
6. **Add validation.** A FluentValidation validator per request DTO, in `Application/Features/<Name>/Validators/`. Validators are auto-registered via `AddValidatorsFromAssemblyContaining<T>()` in `Application`'s `ServiceCollectionExtensions` — no manual registration needed per validator.
7. **Add authorization.** Confirm which permission code (see the `identity.Permissions` catalog in [Database/Migrations/008_Seed_GlobalData.sql](../Database/Migrations/008_Seed_GlobalData.sql)) the endpoint requires, and wire the check at the WebApi layer.
8. **Add audit and logging.** Any mutation on tenant-scoped data should have a clear path to an `audit.AuditLogs` entry once the audit-writing infrastructure exists (Phase 1 of [PROJECT_ROADMAP.md](PROJECT_ROADMAP.md)). Use `ILogger<T>` for operational logging per [CODING_STANDARDS.md](CODING_STANDARDS.md).
9. **Add tests.** At minimum, validator tests and Application-service tests with a faked repository. Integration tests against a real (test) database are encouraged once the stored procedures exist.
10. **Build and review tenant isolation.** Before opening a PR: does every new query filter by `OrganizationId`? Does `OrganizationId` ever come from anywhere other than `IUserContext`? Run through [MULTI_TENANCY.md](MULTI_TENANCY.md)'s checklist.

## Feature completion checklist

- [ ] Domain/Application/Infrastructure/WebApi placement matches [SOLUTION_STRUCTURE.md](SOLUTION_STRUCTURE.md) — no rule from [ARCHITECTURE_RULES.md](ARCHITECTURE_RULES.md) violated.
- [ ] Every new tenant-scoped table/query carries and filters by `OrganizationId`.
- [ ] `OrganizationId` is sourced from `IUserContext`, never from the request body/query/header.
- [ ] New stored procedures live in a new, sequentially-numbered migration file; no existing migration was edited.
- [ ] FluentValidation validator exists for every new request DTO.
- [ ] No secrets, connection strings, or credentials were added to a committed `appsettings*.json`.
- [ ] No raw password, token, or JWT is logged anywhere in the new code.
- [ ] `dotnet build` succeeds with zero new warnings.
- [ ] New/changed behavior has at least a validator test or a service-level test.
- [ ] The feature does not introduce MediatR, CQRS, EF Core, or Vertical Slice Architecture unless that was an explicit, separate decision.

## Working with migrations

- Migrations are plain numbered `.sql` files under `Database/Migrations/`, applied in order. `dbo.SchemaVersions` (introduced in migration `009`) tracks what's been applied — a new migration should check `dbo.SchemaVersions` for its own ID and no-op if already applied, matching the pattern in `009_Harden_MultiTenant_Identity.sql`.
- Never edit a migration that may already be applied somewhere (a teammate's local DB, a shared dev environment). Add a new migration instead.
- Schema ownership: `tenant`, `identity`, `crm`, `audit`, `billing`, `notification`, `education` — see [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md) §2–3 for what goes where.

## Branching and review

- One feature = one focused PR. Don't bundle unrelated cleanup into a feature PR.
- Every PR touching a tenant-scoped table gets a specific reviewer callout: "confirm every new query filters by OrganizationId."
