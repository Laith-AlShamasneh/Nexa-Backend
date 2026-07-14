# Nexa — Domain Model

This documents the `Domain` project as reflected from the database after all migrations `001`–`010` are applied. Every finalized table in [Database/Migrations/](../Database/Migrations) is accounted for below — either as a Domain type or with an explicit reason it isn't one. Source of truth for the schema itself: [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md).

## Bilingual (English/Arabic) name fields (migration 010)

Every user-facing name field has an Arabic counterpart, added alongside the existing (English) column — nothing was renamed:

| English column | Arabic column | Table / Domain type |
|---|---|---|
| `Name` | `ArabicName` | `tenant.Organizations` / `Organization` |
| `LegalName` | `ArabicLegalName` | `tenant.Organizations` / `Organization` |
| `Name` | `ArabicName` | `tenant.Branches` / `Branch` |
| `FirstName` | `ArabicFirstName` | `identity.Persons` / `Person` |
| `LastName` | `ArabicLastName` | `identity.Persons` / `Person` |
| `FullName` (computed) | `ArabicFullName` (computed) | `identity.Persons` / `Person` |
| `Name` | `ArabicName` | `identity.Roles` / `Role` |
| `Name` | `ArabicName` | `identity.Permissions` / `Permission` |
| `DisplayName` | `ArabicDisplayName` | `crm.Customers` / `Customer` |

All Arabic columns are nullable — not every row will have a translation yet, and the English column remains the required/primary field. `ArabicFullName` mirrors the database's computed column exactly: `NULL` when neither `ArabicFirstName` nor `ArabicLastName` is set (not an empty string), otherwise the trimmed concatenation of whichever parts are present — implemented as a read-only C# computed property on `Person`, the same pattern already used for the English `FullName`.

**Not extended to bilingual**: `Username` (a login handle, not a display name), `EntityName` on `AuditLog` (a technical type identifier like `"crm.Customers"`, not human-facing text), `Description`/`Note`/`Address` fields anywhere (the request was specifically about **name** fields — descriptions and free-text content are a separate, broader localization concern not in scope here).

**Note on the pre-existing Authentication feature**: `Application/Features/Authentication/DTOs/RegisterDto.cs` already had its own bilingual convention from before this change — suffixed `FirstNameEn`/`FirstNameAr`, `LastNameEn`/`LastNameAr`, `DisplayNameEn`/`DisplayNameAr` — rather than the `Arabic`-prefixed style used here. That Application-layer code was untouched by this change (it predates the Domain model and isn't wired to it yet — see "Deferred Work" in the Phase 0 report); reconciling the two naming conventions is next-phase work when the Authentication feature is actually connected to `Person`/`User`.

## Domain areas

```
Domain
├── Common/            Entity<TId>, ITenantOwned, ISoftDeletable, IAuditable
├── Exceptions/         DomainException, NotFoundException, ForbiddenException, ValidationAppException (pre-existing)
├── Tenancy/            Organization, Branch, OrganizationSettings
├── Identity/           Person, User, Role, Permission, UserRole, RolePermission,
│                       RefreshToken, EmailConfirmationToken, PasswordResetToken,
│                       UserSession, UserInvitation, SignInLog
├── Customers/          Customer, CustomerNote
└── Auditing/           AuditLog
```

Each area follows the same internal shape: `Entities/`, and `Enums/`/`Constants/` only where the area actually has one (no empty folders were created).

## Table-to-Domain mapping

| SQL Table | Domain Type | Classification | Tenant-Owned | Notes |
|---|---|---|---|---|
| `tenant.Organizations` | `Organization` | Aggregate root | — (it *is* the tenant) | `OrganizationStatus` enum (0-3, CK-restricted); `ArabicName`/`ArabicLegalName` (010) |
| `tenant.Branches` | `Branch` | Aggregate root (own lifecycle, queried independently) | Yes (`ITenantOwned`) | `BranchStatus` enum (0/1); `ArabicName` (010); "one main branch" is a cross-aggregate invariant, documented not enforced here |
| `tenant.OrganizationSettings` | `OrganizationSettings` | Aggregate root (1:1 extension, own Dapper query) | Yes — `OrganizationId` is both Id and FK | No soft delete; lives/dies with its Organization |
| `identity.Persons` | `Person` | Aggregate root | Yes (`ITenantOwned`) | `Gender` reuses `Shared.Enums.Identity.GenderTypes`; `FullName`/`ArabicFullName` are computed C# properties (not DB-mirrored fields); `ArabicFirstName`/`ArabicLastName` (010) |
| `identity.Users` | `User` | Aggregate root | Yes (`ITenantOwned`) | `NormalizedEmail`/`NormalizedUsername` are DB-computed, read-only, populated only via `Reconstitute` |
| `identity.Roles` | `Role` | Aggregate root | **No** — `OrganizationId` is nullable (global template vs. tenant-local); exposed directly, not via `ITenantOwned` | `TemplateRoleId` (009) links a tenant clone back to its template; `ArabicName` (010) |
| `identity.Permissions` | `Permission` | Lookup/catalog | No (global) | Read-only: seeded by migration 008's `MERGE`, never created/edited through app code — no `Create`, only `Reconstitute`. See `Constants.PermissionCodes`. `ArabicName` (010) |
| `identity.RolePermissions` | `RolePermission` | Persistence-only relationship, modeled | No | `sealed record`, composite key (RoleId, PermissionId) — see §5 below |
| `identity.UserRoles` | `UserRole` | Persistence-only relationship, modeled | Yes (denormalized `OrganizationId`, not via `ITenantOwned` since no single-Id base fits a composite key) | `sealed record`, composite key (UserId, RoleId) — see §5 below |
| `identity.RefreshTokens` | `RefreshToken` | Supporting entity | Yes (`OrganizationId` property, no `ITenantOwned` — composite-key-free but see note below) | Only the hash is stored; `TokenFamilyId`/`ReplacedByTokenId`/`SessionId` (009) back rotation + reuse detection |
| `identity.EmailConfirmationTokens` | `EmailConfirmationToken` | Supporting entity | Yes | `OrganizationId`/revocation fields added in 009 |
| `identity.PasswordResetTokens` | `PasswordResetToken` | Supporting entity | Yes | Same shape as above |
| `identity.UserSessions` | `UserSession` (009, new) | Supporting entity | Yes (`ITenantOwned`) | Backs "log out everywhere" |
| `identity.UserInvitations` | `UserInvitation` (009, new) | Supporting entity | Yes (`ITenantOwned`) | "Invite a teammate" flow |
| `identity.SignInLogs` | `SignInLog` | Append-only security record | **No** — `OrganizationId`/`UserId` both nullable | No FK in the database by design; no update/delete methods; `Successful(...)`/`Failed(...)` factories |
| `crm.Customers` | `Customer` | Aggregate root | Yes (`ITenantOwned`) | `CustomerStatus` enum (0/1/2); `CustomerType` stays a plain guarded string (vertical-defined label), never an enum; `ArabicDisplayName` (010) |
| `crm.CustomerNotes` | `CustomerNote` | Supporting entity | Yes (`ITenantOwned`) | `CreatedBy` is required (not nullable, unlike most other entities); gained `DeletedAt`/`DeletedBy` in 009 |
| `audit.AuditLogs` | `AuditLog` | Append-only operational record | **No** — `OrganizationId`/`UserId` both nullable | No FK in the database by design; no update/delete methods; `Record(...)` factory |
| `dbo.SchemaVersions` (009) | *(none)* | Not modeled | N/A | Ops/migration-tracking artifact, not a business concept — see "Intentionally not modeled" below |

## Aggregate root decisions

**Organization, Branch, OrganizationSettings, Person, User, Role, Customer** are aggregate roots: each has its own identity, its own lifecycle/invariants, and — because Dapper never hydrates an object graph — each is loaded and persisted independently rather than as part of a parent's collection. `Branch` and `OrganizationSettings` reference `Organization` by Id only; neither is a child collection *on* `Organization`.

## Non-rich / simple models

- **`Permission`** is a read-only catalog row. It has no business behavior because none exists in the product: permissions are seeded once by SQL and never mutated through the application. Only `Reconstitute` exists, no `Create`.
- **`SignInLog`, `AuditLog`** are append-only. No `Update`/`Delete` method exists on either type, matching the database's lack of an `UpdatedAt` column and (for AuditLog) the explicit "append-only" design note in the blueprint. Their factories (`SignInLog.Successful/Failed`, `AuditLog.Record`) exist only to make correct construction easy, not to add business rules that don't exist.

## Relationship tables — `UserRoles` and `RolePermissions`

Both are modeled as **explicit `sealed record` types**, not skipped:

- **`UserRole`** carries real audit-relevant data beyond the bare join (`AssignedAt`, `AssignedBy`), which Application/Infrastructure will want for "who granted this role" queries — that's reason enough for an explicit type per the task's guidance ("if the project's Dapper mapping strategy requires an explicit class, create a small explicit model").
- **`RolePermission`** is a purer join (`RoleId`, `PermissionId`, `CreatedAt`) but is still modeled explicitly, for symmetry with `UserRole` and because Dapper needs *some* materialization target for the join query results either way.

Both are `record`s rather than `Entity<TId>` subclasses because neither has a single scalar identity (their identity is the composite key) and neither has any state that changes after creation — assignment/grant or revoke-by-delete are the only two states, matching "prefer records for immutable data" from [CODING_STANDARDS.md](CODING_STANDARDS.md).

## Tenant safety — how immutable `OrganizationId` ownership is represented

- `ITenantOwned` (`Domain/Common/ITenantOwned.cs`) exposes `Guid OrganizationId { get; }` — get-only, set once by the entity's own constructor, and implemented by every entity whose tenant ownership is unconditionally required: `Branch`, `Person`, `User`, `Customer`, `CustomerNote`, `UserSession`, `UserInvitation`.
- **No entity anywhere exposes a setter for `OrganizationId`** — there is no `ChangeOrganization` method on any type. Tenant reassignment is not a modeled operation, matching [ARCHITECTURE_RULES.md](ARCHITECTURE_RULES.md) rule 10 and [MULTI_TENANCY.md](MULTI_TENANCY.md).
- **Documented exceptions** (entities that do *not* implement `ITenantOwned`, because the database itself makes `OrganizationId` legitimately nullable): `Role` (`null` = global system template), `SignInLog` and `AuditLog` (`null` = platform-level event, or an unresolved tenant on a failed login). These expose `Guid? OrganizationId` directly instead, with an XML doc comment explaining why.
- `RefreshToken`, `EmailConfirmationToken`, `PasswordResetToken` expose a required `Guid OrganizationId` property but do not formally implement `ITenantOwned` — this was a scope call, not a modeling disagreement: nothing currently needs to treat these polymorphically as "any tenant-owned thing," so adding the interface was deferred rather than speculative. It can be added later with no behavior change if a use case needs it.
- **Cross-table tenant consistency is not something any single Domain object can guarantee.** A `UserRole` cannot verify that its `User` and `Role` actually belong to the same organization by itself — that is enforced by the database's composite tenant-safe foreign keys (migration 009) and must be respected by Application-layer workflows that construct these objects. This is documented per-entity in XML doc comments and is restated in [MULTI_TENANCY.md](MULTI_TENANCY.md).

## Cross-aggregate invariants (documented, not enforced by a single entity)

- **At most one active main Branch per Organization** — `Branch.SetAsMainBranch()` cannot check sibling branches; enforced by `UX_Branches_Organization_MainBranch` and the Application workflow that calls it.
- **Refresh-token reuse detection** — revoking an entire `TokenFamilyId` on reuse of an already-revoked token requires looking up sibling tokens; `RefreshToken.Revoke`/`Replace` only manage the one instance.
- **Tenant-consistent role assignment** — a `UserRole` must reference a `Role` and `User` in the same `OrganizationId`; enforced by composite FKs, not by `UserRole` itself.
- **Tenant role materialization** — cloning the four system role templates into a new organization's own `Role` rows (with `TemplateRoleId` set) is a multi-entity Application workflow, not a single `Role` method.

## Intentionally not modeled yet

- **`dbo.SchemaVersions`** — an operational/migration-tracking table, not a business concept. No Domain type.
- **Course, Enrollment, Attendance, Payment, Service** — explicitly out of scope per the task; `Customer` does not reference or anticipate any of these (see [PROJECT_ROADMAP.md](PROJECT_ROADMAP.md) Phases 6-8).
- **Value objects** (`EmailAddress`, `PhoneNumber`, `CurrencyCode` as dedicated types) — every email/phone/currency-code field is a guarded `string` for now. Candidates worth revisiting once real validation/formatting logic accumulates: `EmailAddress` (used on `Person`, `User`, `Branch`, `Organization`, `UserInvitation`), `CurrencyCode` (used on `OrganizationSettings`, already has an ISO-4217-length guard that could become a value object's constructor invariant instead).
- **Row-Level-Security-aware base type** — deferred along with RLS itself (see [MULTI_TENANCY.md](MULTI_TENANCY.md) "Future Row-Level Security considerations").

## ID types (per the actual SQL column types, not assumed)

| CLR type | Used by |
|---|---|
| `Guid` | `Organization`, `Branch`, `OrganizationSettings` (Id = OrganizationId), `Person`, `User`, `Role`, `UserSession`, `Customer` — all `UNIQUEIDENTIFIER` PKs |
| `long` | `RefreshToken`, `EmailConfirmationToken`, `PasswordResetToken`, `UserInvitation`, `SignInLog`, `AuditLog`, `CustomerNote` — all `BIGINT IDENTITY` PKs |
| `int` | `Permission` — `INT IDENTITY` PK |
| *(composite, no surrogate)* | `UserRole` (UserId, RoleId), `RolePermission` (RoleId, PermissionId) |

**Known cross-layer inconsistency, not fixed by this task**: `Application.Interfaces.Services.IUserContext.UserId` and the Authentication feature's DB models (`Application/Features/Authentication/DbModels/*`) still use `long` for user identifiers, left over from the previous cleanup pass. `Domain.Identity.Entities.User.Id` is `Guid`, matching the actual database column. This mismatch must be resolved in Application/Infrastructure before the Identity phase ships — see the Phase 0 cleanup report's "Remaining Risks" and [README.md](../README.md) "Current limitations."

## Dapper construction/materialization strategy

**Dapper never materializes a `Domain` type directly.** Every entity's constructor is `private`; there is no public parameterless constructor and no exposed public setters for Dapper's reflection-based mapper to use. Two static factories exist per entity instead:

- **`Create(...)` / a more specific verb** (`IssueNew`, `Open`, `Issue`, `Record`, `Assign`, `Grant`) — builds a brand-new instance, validating creation invariants. For `Guid`-keyed entities this **generates the Id client-side** via `Guid.CreateVersion7()` (a time-ordered v7 GUID, deliberately chosen to match what `NEWSEQUENTIALID()` is trying to achieve at the database level — see docs/database/DATABASE_FINAL_BLUEPRINT.md). Infrastructure then inserts that exact Id rather than relying on the column default. For `long`/`int`-keyed entities backed by `IDENTITY` columns, the entity is constructed with `Id = 0` and a guarded `AssignDatabaseId(long id)` method is called exactly once, after the INSERT returns the database-generated value (via `OUTPUT INSERTED.Id` or `SCOPE_IDENTITY()`); calling it twice throws `DomainException`.
- **`Reconstitute(...)`** — rebuilds an entity from already-persisted state (every column, including the Id). This is the factory Infrastructure repositories call after a Dapper query returns a row. It performs no re-validation (the data already passed validation once, on the way in) and exists purely to hand raw column values to a constructor Dapper itself can never reach.

This means Infrastructure's actual mapping code (not built in this task — see Deferred Work) will typically: (1) `Dapper.Query<TRow>(...)` into a small Infrastructure-only row-shaped type (a plain record with public properties, *not* the Domain entity), then (2) call the matching `Reconstitute(...)` factory to build the real `Domain` entity from that row. This keeps 100% of Domain's construction path free of any Dapper awareness, direct or indirect.

## Length constants

Column-length guards live next to the area they describe, matching actual `NVARCHAR`/`CHAR` sizes from the migrations — `Domain/Tenancy/Constants/TenancyLengths.cs`, `Domain/Identity/Constants/IdentityLengths.cs`, `Domain/Customers/Constants/CustomerLengths.cs`, `Domain/Auditing/Constants/AuditLengths.cs`. No single monolithic `DatabaseConstants` class, per [CODING_STANDARDS.md](CODING_STANDARDS.md).

## Equality

`Entity<TId>` implements identity-based equality (same `Id` ⇒ same entity, regardless of other property values) via `IEquatable<Entity<TId>>` plus `==`/`!=` operator overloads. `UserRole`/`RolePermission` use `record`'s built-in structural equality instead, which is correct for them since their "identity" *is* their full (small) set of key fields.

## Confirmed by Tenant Onboarding (first real consumer of this model)

[docs/TENANT_ONBOARDING.md](TENANT_ONBOARDING.md) is the first Application-layer workflow to actually construct these entities, and it confirms the design holds up in practice rather than just on paper:

- `Organization.Create`, `Branch.Create`, `OrganizationSettings.CreateDefault` + `UpdateLocale`, `Person.Create`, and `User.Create` are all called from `TenantOnboardingService` — each one's client-side `Guid.CreateVersion7()` Id becomes the actual `@OrganizationId`/`@BranchId`/`@PersonId`/`@UserId` parameter passed into `tenant.usp_Organization_Register`, exactly as this document's "Dapper construction strategy" predicted.
- `Role`'s tenant-local-clone-from-global-template model (§ above) is implemented entirely in SQL (set-based `INSERT ... SELECT`), not through the `Role.CreateTenantRole` factory — cloning dozens of permission rows one Domain object at a time would mean a C# loop the stored procedure can do in one set-based statement instead. `Role.CreateTenantRole` remains available for a future workflow that creates a single custom tenant role interactively (not a bulk clone).
- `UserRole`/`RolePermission` records are, similarly, never constructed in C# for this workflow — the bulk assignment/grant is SQL-side. The record types stay as the read-side shape for whenever Application needs to query "which roles does this user have" later.
