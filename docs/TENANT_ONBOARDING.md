# Nexa — Tenant Onboarding

## Business purpose

The first workflow a new institute or training center performs: register once and receive a complete, working tenant — an Organization, its main Branch, default settings, and an Owner account that can sign in (after confirming email) and start managing the business. Every later module (Identity/Login, CRM, Billing, ...) depends on this having run first.

## Public endpoint

```
POST /api/organizations/register
```

No authentication required — this is the one endpoint that creates the tenant a JWT would otherwise scope requests to (see "Multi-tenant considerations" below). Rate-limited at 5 requests/minute per IP (`RateLimiterPolicies.PublicRegistration` in `WebApi/Program.cs`) since it is public and unauthenticated.

### Request fields

| Field | Required | Notes |
|---|---|---|
| `OrganizationName` | Yes | |
| `OrganizationArabicName`, `OrganizationLegalName`, `OrganizationArabicLegalName` | No | |
| `LogoUrl` | No | A URL string, not a file upload — see "Current limitations" |
| `OrganizationEmail`, `OrganizationPhone`, `OrganizationAddress` | No | |
| `TimeZoneId` | Yes | Validated against the .NET/OS IANA time zone database |
| `DefaultLanguageCode` | Yes | `xx` or `xx-YY` form, e.g. `ar-JO`, `en-US` |
| `CurrencyCode` | Yes | 3 uppercase letters (ISO 4217 shape), e.g. `JOD` |
| `BranchName` | Yes | Becomes the main branch |
| `BranchArabicName`, `BranchPhone`, `BranchEmail`, `BranchAddress` | No | |
| `FirstName`, `LastName` | Yes | Owner's name |
| `ArabicFirstName`, `ArabicLastName` | No | |
| `Username`, `Email` | Yes | Owner's login credentials |
| `Phone` | No | Owner's phone |
| `Password`, `ConfirmPassword` | Yes | Must match; password policy below |

**Never accepted** (system-generated only): `OrganizationId`, `PersonId`, `UserId`, `BranchId`, `RoleId`, permission IDs, `IsEmailConfirmed`, `IsActive`, `PasswordHash`, `NormalizedEmail`/`NormalizedUsername`, `CreatedBy`/audit fields, system-role flags, subscription internals. The request DTO (`Application/Features/Tenancy/DTOs/RegisterOrganizationDtos.cs`) simply has no properties for any of these.

**Slug is not a request field.** `tenant.Organizations.Slug` is the real unique tenant identifier, but a non-technical business owner shouldn't need to think about it at signup. `SlugGenerator.FromName` derives it from `OrganizationName` (lowercased, non-alphanumeric collapsed to hyphens) plus a short random suffix, so two organizations with the same name never collide. The database's `UX_Organizations_Slug` unique index remains the final safety net regardless.

### Response

```json
{
  "success": true,
  "code": 201,
  "message": "...",
  "result": {
    "organizationId": "...",
    "mainBranchId": "...",
    "ownerUserId": "...",
    "ownerEmail": "owner@example.com",
    "emailConfirmationRequired": true,
    "createdAt": "2026-..."
  }
}
```

Never returned: password hash, the confirmation token (raw or hashed), the owner's Role Id, or any SQL/internal detail.

### HTTP status vs. body code

Every handled outcome — success, validation failure, conflict — is returned as **HTTP 200**. The status the frontend should actually branch on is `code` inside the body (`201` success, `400` validation, `409` conflict, `500` internal). Only a genuinely unhandled exception (escaping to `GlobalExceptionHandler`) or the rate limiter rejecting a request return a non-200 HTTP status — see `WebApi/Common/ApiResponseExtensions.cs` for the mapping and rationale.

## Workflow sequence

1. **WebApi** (`OrganizationEndpoints.RegisterAsync`) validates the request via `RegisterOrganizationValidator` (FluentValidation). A failure returns `ApiResponse.Fail(400, ...)` wrapped in HTTP 200.
2. **Application** (`TenantOnboardingService.RegisterAsync`):
   a. Builds Domain entities (`Organization.Create`, `Branch.Create`, `OrganizationSettings.CreateDefault` + `UpdateLocale`, `Person.Create`) — this runs each entity's own creation invariants as a second guard beyond FluentValidation, and generates the client-side `Guid.CreateVersion7()` IDs the whole tenant will use.
   b. Hashes the password (`IPasswordHasher.Hash`) and builds `User.Create` with the hash — **before** any database call.
   c. Generates a raw email-confirmation token and hashes it (`ITokenHasher.GenerateRawToken`/`Hash`) — also before the database call. The raw token exists only in memory from here until it's placed in the confirmation link.
   d. Calls `IOrganizationRegistrationRepository.RegisterAsync` — one Dapper call to the stored procedure.
   e. Maps the procedure's `ResultCode` to a `ServiceResult`.
   f. On success, enqueues the confirmation email via the existing `IBackgroundJobService` + `EmailConfirmationPayload` + `JobTypes.EmailConfirmation` (all pre-existing, reused as-is — see "Reused vs. new" below).
3. **Infrastructure** (`OrganizationRegistrationRepository`) executes `tenant.usp_Organization_Register` via `IDbExecutor`, `CommandType.StoredProcedure`, one round trip.
4. **SQL Server** runs the entire write set in one transaction (see below) and returns one result row.

## Transaction boundary

One stored procedure, one transaction, one round trip — `tenant.usp_Organization_Register` (migration 011). `SET XACT_ABORT ON` plus an explicit `BEGIN TRANSACTION`/`COMMIT`/`ROLLBACK` in `TRY/CATCH` means any failure partway through rolls back everything already written in that call. Two cheap pre-checks (organization slug conflict; global role templates present) run *before* `BEGIN TRANSACTION`, so an expected failure never even opens a transaction.

### What the procedure writes, in order

1. `tenant.Organizations` — the new tenant, `Status = 0 (Trial)`.
2. `tenant.Branches` — the main branch, `IsMainBranch = 1`, `Status = 1 (Active)`.
3. `tenant.OrganizationSettings` — time zone / language / currency from the request.
4. `identity.Persons` — the owner's personal details.
5. `identity.Users` — the owner's login account, `IsEmailConfirmed = 0`, `IsActive = 1`, `FailedLoginAttempts = 0`, no lockout.
6. `identity.Roles` — **set-based** `INSERT ... SELECT` cloning all four global system templates (Owner/Admin/Accountant/Teacher) into tenant-local rows, each linked back via `TemplateRoleId`. Captured into a table variable (`OUTPUT ... INTO @NewRoles`) so the next step can reference the new Ids without a second query.
7. `identity.RolePermissions` — **set-based** `INSERT ... SELECT ... JOIN` copying each cloned role's permissions from its template. No C# loop over permissions.
8. `identity.UserRoles` — assigns the owner to the tenant's own cloned **Owner** role (never the global template — see "Role initialization" below).
9. `identity.EmailConfirmationTokens` — the hash only; `ExpiresAt` per `AuthenticationOptions.EmailConfirmationExpiryHours`.
10. `audit.AuditLogs` — six rows (`Organizations`/`Branches`/`Users` created, `Roles` initialized, `UserRoles` assigned, `EmailConfirmationTokens` created), `Source = 'TenantOnboarding'`, no secrets.

### Return contract

One result row: `ResultCode`, `OrganizationId`, `BranchId`, `OwnerPersonId`, `OwnerUserId`, `OwnerRoleId`, `EmailConfirmationTokenId`, `CreatedAt`. No hashes, no tokens.

`ResultCode`: `0` = success; `1` = organization slug conflict; `2` = required global role templates missing (a seed-data/deployment defect — Application logs this at `Critical`, not a user-facing validation error).

## Role initialization

Global role templates (`identity.Roles` where `OrganizationId IS NULL`) are never assigned to a user directly. The procedure clones one tenant-local copy of each template for the new organization and assigns the owner to *that* clone's Owner role. This matches the tenant-role model already established in migration 009 — see `docs/database/DATABASE_FINAL_BLUEPRINT.md` §2 and `docs/DOMAIN_MODEL.md`.

## Email confirmation behavior

A raw, cryptographically random token is generated by `ITokenHasher.GenerateRawToken()` (existing Infrastructure service). Only its SHA-256 hash reaches the stored procedure and the database. The raw token is embedded in a confirmation link and handed to the existing `EmailConfirmationPayload`/`JobTypes.EmailConfirmation`/`EmailConfirmationHandler` pipeline (all pre-existing — reused, not duplicated). **The Confirm Email endpoint itself is not implemented in this task** — see "Next recommended module."

## Error cases

| Situation | HTTP | Body `code` | Notes |
|---|---|---|---|
| Validation failure (missing/invalid field) | 200 | 400 | FluentValidation errors, one per field |
| Organization slug conflict | 200 | 409 | Practically only reachable if two orgs share an identical generated slug — astronomically rare given the random suffix |
| Global role templates missing | 200 | 500 | Deployment/seed-data defect, logged at `Critical` |
| Unhandled exception (DB unreachable, etc.) | 500 | — | `GlobalExceptionHandler`, no SQL details leaked |
| Too many requests from one IP | 429 | — | Rate limiter, not a business outcome |

Owner username/email conflicts are **not a reachable business case at registration time**: uniqueness for both is scoped per-organization (`UX_Users_OrganizationId_NormalizedEmail`, `UX_Users_OrganizationId_NormalizedUsername`), and a brand-new organization has zero existing users by construction — there is nothing for the new owner's email/username to conflict with. This is a deliberate consequence of the tenant-isolation model, not an oversight; documented here so it isn't "discovered" as a missing check later.

## Security controls

- **Password**: hashed via the existing `IPasswordHasher` (BCrypt) before the database call; the plaintext never reaches Infrastructure/SQL. Policy: 8+ chars, upper/lower/digit/special (same policy as the existing Authentication feature).
- **Email confirmation token**: generated and hashed via the existing `ITokenHasher`; only the hash is ever persisted, logged, or passed to SQL.
- **CPU work before the transaction**: password hashing and token generation happen in `TenantOnboardingService` *before* calling the repository, so the database transaction — which holds locks — is as short as possible (SQL work only).
- **No client-supplied identifiers**: `OrganizationId`/`PersonId`/`UserId`/`BranchId`/`RoleId` are all generated server-side (`Guid.CreateVersion7()` in the Domain entity factories); the request DTO has no fields for them.
- **Rate limiting**: 5 requests/minute per IP on this endpoint (`WebApi/Program.cs`). See "Current limitations" — this is a baseline, not the final anti-abuse posture.
- **Audit**: six rows per successful registration, `Source = 'TenantOnboarding'`; no password hash, token hash, or raw token in any audit row.
- **Logging**: registration start/success/failure and the new `OrganizationId` are logged structurally; password, password hash, raw token, and token hash are never logged (verified — see the final report's "search for secret logging").

## Multi-tenant considerations

This endpoint is the **one documented exception** to "tenant context always comes from the authenticated JWT" (see `docs/MULTI_TENANCY.md` — updated with this exception). There is no tenant yet, so there is nothing for a JWT to scope. The new `OrganizationId` is generated inside `TenantOnboardingService` (never accepted from the client) and every row the stored procedure writes uses that same value — Organizations, Branches, OrganizationSettings, Persons, Users, the cloned Roles/RolePermissions, UserRoles, and EmailConfirmationTokens all key off the one `@OrganizationId` parameter. The procedure only ever reads two things outside the new tenant: the global (`OrganizationId IS NULL`) role templates and permissions — both read-only, both intentionally shared across all tenants.

## Current limitations

- **Email provider may still be a development implementation** — `IEmailService`'s concrete SMTP sender depends on `Smtp:*` configuration being set for a real provider; unconfigured, sends will fail (the background job retries per its existing retry policy) but registration itself still succeeds.
- **Confirm-email endpoint is not yet implemented** — the token is created and the link is sent, but there is no endpoint yet to consume it. Recommended next module (see below).
- **Subscription billing is deferred** — `SubscriptionPlanCode` stays `NULL`; the organization starts in `Trial` status only.
- **Advanced anti-abuse controls are deferred** — no CAPTCHA, no device fingerprinting, no email-domain verification. The 5-req/min-per-IP limit is a practical baseline for now.
- **`LogoUrl` accepts a URL string, not a file upload** — pre-auth multipart upload wasn't built for this endpoint; a URL is stored as-is if provided.
- **`CountryCode` was not added** — the finalized `tenant.Organizations` schema has no backing column for it; rather than invent an unreviewed column, it was left out of the request. `TimeZoneId`/`CurrencyCode` carry the equivalent locale information the schema actually supports.

## Next recommended module

**Email Confirmation** — the endpoint that consumes the token this workflow creates (`identity.EmailConfirmationTokens`), sets `Users.IsEmailConfirmed = 1`, and is the prerequisite for the (also not-yet-implemented) Login flow. Not implemented as part of this task.
