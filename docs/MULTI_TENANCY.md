# Nexa — Multi-Tenancy

## Strategy: Shared Database + Shared Schema + OrganizationId

Every tenant (an "Organization" — see [PRODUCT_CONTEXT.md](../PRODUCT_CONTEXT.md)) shares the same database and the same tables as every other tenant. Isolation is enforced by an `OrganizationId` column present on every tenant-scoped table, not by separate databases or separate schemas per tenant. Full rationale and DDL: [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md).

## OrganizationId as TenantId

`OrganizationId` **is** the tenant ID throughout the codebase — there is no separate "TenantId" concept layered on top. `tenant.Organizations.Id` (a `UNIQUEIDENTIFIER`) is the single source of truth.

## Tenant resolution

1. A user authenticates and is issued a JWT whose claims include `org_id` — the organization that user's account belongs to (`identity.Users.OrganizationId`).
2. Every subsequent request carries that JWT.
3. `Infrastructure/Services/Authentication/UserContext.cs` (implementing `Application.Interfaces.Services.IUserContext`) reads `org_id` from the authenticated `ClaimsPrincipal` and exposes it as `Guid? OrganizationId`.
4. Application services and Infrastructure repositories read `IUserContext.OrganizationId` — they never re-derive it from anywhere else.

## Pre-authentication tenant creation (the one documented exception)

`POST /api/organizations/register` (Tenant Onboarding — see [docs/TENANT_ONBOARDING.md](TENANT_ONBOARDING.md)) is the **single exception** to "tenant context always comes from the authenticated JWT." There is no tenant yet at this point, so there is no `org_id` claim to read — the endpoint's entire job is to create the tenant a future JWT would reference.

How the exception stays safe:

- **`OrganizationId` is generated internally, never accepted from the request.** `TenantOnboardingService` calls `Organization.Create(...)`, which generates its own `Guid.CreateVersion7()` Id — `Application/Features/Tenancy/DTOs/RegisterOrganizationDtos.cs` has no `OrganizationId` property for a client to populate even by mistake.
- **Every row the registration writes uses that one generated value.** `tenant.usp_Organization_Register` (migration 011) takes `@OrganizationId` as a single input parameter and every INSERT — Organizations, Branches, OrganizationSettings, Persons, Users, cloned Roles, cloned RolePermissions, UserRoles, EmailConfirmationTokens, AuditLogs — uses that same parameter. There is no path in the procedure where a row could be written against a different `OrganizationId`.
- **Role-template cloning reads global data only.** The only rows this workflow reads *outside* the new tenant are the four system role templates and their permissions (`identity.Roles`/`RolePermissions` where `OrganizationId IS NULL`) — both intentionally global and read-only from every tenant's perspective, not another tenant's private data.
- **The transaction is atomic.** See [docs/TENANT_ONBOARDING.md](TENANT_ONBOARDING.md) "Transaction boundary" — a failure partway through leaves no partial tenant behind for a subsequent request to accidentally attach to.
- **No endpoint after this one accepts a client-supplied `OrganizationId` either** — this exception is scoped to the one operation that creates a tenant, not a general precedent for trusting client-supplied tenant IDs anywhere else in the platform.

## The CurrentTenant / CurrentUser abstraction

`IUserContext` is the single seam through which "who is making this request, and for which organization" flows into Application and Infrastructure code:

```csharp
public interface IUserContext
{
    long UserId { get; }
    Guid? OrganizationId { get; }   // resolved from the JWT's "org_id" claim — see below
    // ... Email, DisplayName, RoleId, Language, IpAddress, ...
}
```

Every tenant-scoped repository method takes an explicit `OrganizationId` parameter rather than reaching into `IUserContext` internally — the calling Application service reads it once from `IUserContext` and passes it down. This keeps repository methods testable without a fake `HttpContext` and makes the tenant boundary visible at every call site.

## Why OrganizationId must never be trusted from a request body/header/query string

If a client could pass `organizationId` in a request payload and have it honored, any authenticated user could read or write another tenant's data simply by changing that value. This is the single most damaging class of bug a multi-tenant SaaS product can ship — indistinguishable from a full authentication bypass from the affected tenant's point of view. **The only trusted source of `OrganizationId` for any tenant-scoped operation is `IUserContext.OrganizationId`, derived from the signed JWT.** A request field named `organizationId` (if one exists at all, e.g. for a platform-admin "act as tenant X" tool) must be treated as untrusted input and explicitly authorized, never used directly as the isolation filter.

## Repository / query filtering requirements

- Every stored procedure that touches a tenant-scoped table takes `@OrganizationId` as a required parameter and includes it in the `WHERE` clause (or join condition) — no exceptions, no "it's probably fine because the caller already filtered."
- Every repository interface method for a tenant-scoped entity takes an explicit `organizationId` parameter — this is enforced by convention and code review today; `docs/database/DATABASE_FINAL_BLUEPRINT.md` §7 describes the planned second layer (SQL Server Row-Level Security keyed on `SESSION_CONTEXT('OrganizationId')`) as defense-in-depth once the identity/session plumbing to set that session context exists.

## Cross-tenant foreign-key strategy

As of [migration 009](../Database/Migrations/009_Harden_MultiTenant_Identity.sql), tenant-scoped tables reference their tenant-scoped parents through **composite tenant-safe foreign keys** — `(OrganizationId, ChildFkId) → Parent(OrganizationId, Id)` against a composite unique index on the parent — rather than a plain `Id → Id` FK. This makes a cross-tenant reference (e.g. a `UserRoles` row pointing at a `User` in a different organization than the row's own `OrganizationId`) a constraint violation at the database level, not just an application-layer convention. See the blueprint §4 and §7 for the full list of tables this applies to.

## Tenant-aware uniqueness

Uniqueness that would naturally be global in a single-tenant system (a user's email, a branch code, a customer code) is scoped per tenant via filtered unique indexes: `UX_Users_OrganizationId_NormalizedEmail`, `UX_Branches_OrganizationId_Code`, `UX_Customers_OrganizationId_CustomerCode`. The same email can be a valid login at two different organizations — see the blueprint §5 for why this is the intended design, not an oversight.

## Tenant onboarding overview

**Implemented.** New-organization signup runs as a single transaction (`tenant.usp_Organization_Register`, migration 011): create `Organizations` → create the main `Branches` row → create `OrganizationSettings` → create the owner's `Persons`/`Users` rows → clone the system role templates into tenant-local `Roles` rows → clone `RolePermissions` → assign the tenant's own `Owner` role → issue an email-confirmation token → write audit rows. Full workflow, request/response contract, and error cases: [docs/TENANT_ONBOARDING.md](TENANT_ONBOARDING.md). Schema rationale: [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md) §5.

## Background-job tenant handling

`identity.RefreshTokens`, `SignInLogs`, and similar tables carry a denormalized `OrganizationId` even where not strictly needed for a foreign key, specifically so background jobs and cleanup services (`NotificationCleanupService`, `BackgroundJobProcessor`) can filter/report per tenant without a join. Any new background job that touches tenant data must accept/filter by `OrganizationId` exactly like a request-scoped repository call would — a job has no `HttpContext` to derive it from, so it must be threaded through the job payload explicitly (see `Application.Features.Email.Jobs.*Payload` records, which already carry the data they need rather than re-deriving it).

## Cache-key tenant isolation

`ICacheService` keys must always be tenant-scoped for any cached value that is organization-specific — e.g. `$"sstamp:{userId}"` is safe because a `UserId` is already tenant-unique in practice, but a hypothetical `$"active-courses"` key would leak across tenants and must be `$"active-courses:{organizationId}"` instead. When adding a new cache entry, ask: "could two different organizations produce the same key for different data?" If yes, the key is missing its tenant segment.

## File-storage tenant isolation

`IStorageUtility.BuildFileKey` should include the owning organization in the storage path/key for any tenant-owned file (not just a folder-type enum) once file uploads move beyond the current single-tenant profile-picture case, so that a signed URL or key guess can never resolve to another tenant's file.

## Logging tenant context

Structured log entries for any tenant-scoped operation should include `OrganizationId` as a structured property (not string-interpolated) so tenant-scoped issues can be filtered in log aggregation — this is not yet wired up (Phase 1 of the roadmap) but should be added alongside the audit-logging foundation.

## Testing tenant isolation

- Unit tests for any repository/service should include a case asserting that a call scoped to Organization A cannot see/modify a row belonging to Organization B.
- Before merging a PR that adds a new tenant-scoped table or query, verify: (1) the table has `OrganizationId NOT NULL`, (2) every SP/query filters by it, (3) any FK to another tenant-scoped table is a composite tenant-safe FK.

## Future Row-Level Security considerations

SQL Server Row-Level Security (a `FILTER`/`BLOCK` security predicate keyed on `SESSION_CONTEXT('OrganizationId')`) is the planned second database-level defense layer, added once the connection-per-request plumbing sets that session context via `sp_set_session_context` right after authentication. This is deliberately not implemented yet — see [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md) §7 for the full four-layer model (application filtering, composite FKs, RLS, JWT claim validation) and its current status.

## Critical examples of tenant-data leakage (what NOT to do)

- ❌ An endpoint reads `organizationId` from the request body/query string and passes it straight to a repository call. **Any** authenticated user could then read/write another tenant's data by changing that value.
- ❌ A repository method takes only an entity `Id` (e.g. `GetCustomerAsync(Guid id)`) with no `organizationId` parameter, relying on the caller "already having filtered." A single missed filter upstream leaks cross-tenant data.
- ❌ A cache key or file-storage key omits `OrganizationId` for tenant-owned data, allowing Organization A's cached/stored data to be served to Organization B under key collision.
- ❌ A background job processes "all rows past their due date" without an `OrganizationId` scope, sending Organization A's reminder email using Organization B's branding/template data because it read the wrong settings row.
- ❌ A new foreign key from one tenant-scoped table to another uses a plain `Id → Id` reference instead of the composite tenant-safe pattern, silently allowing a data-entry bug to link rows across tenants without triggering any constraint.
