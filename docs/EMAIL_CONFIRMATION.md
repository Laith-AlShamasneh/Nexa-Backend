# Nexa — Email Confirmation

## Business purpose

A newly registered organization owner must confirm their email address before
they can sign in (Login is a separate, not-yet-implemented module — see
`docs/PROJECT_ROADMAP.md`). Tenant Onboarding (`tenant.usp_Organization_Register`,
migration 011) already creates the owner's first confirmation token and sends the
email; this module is what *consumes* that token, and what lets a user request a
new one if the original expired, was lost, or never arrived.

Two public, pre-authentication endpoints:

```
POST /api/auth/confirm-email
POST /api/auth/resend-email-confirmation
```

Both are documented exceptions to "tenant context always comes from the JWT" (see
`docs/MULTI_TENANCY.md`) — there is no JWT yet for an unconfirmed user, exactly like
organization registration. Neither endpoint accepts an `OrganizationId` or `UserId`
from the client; both are resolved entirely server-side from the token hash or the
email address.

## Relationship to `AuthService`

`AuthService` (Login/Register/ChangePassword/...) used to have its own
`ConfirmEmailAsync`/`ResendConfirmationEmailAsync` methods. They were **removed**,
not adapted, because they called stored procedures that don't exist in any
migration (`identity.usp_Authentication_ConfirmEmail`,
`...SaveConfirmationToken`, `...GetUserConfirmationStatus`) and used a `long
UserId`-based DbModel shape left over from before the schema settled on
`UNIQUEIDENTIFIER` keys. No WebApi endpoint ever called them — confirmed by grep
before removal — so deleting them removed dead, broken code, not a working
abstraction. `docs/SECURITY_BASELINE.md` already documented this gap ("AuthRepository's
own confirm-email stored procedures still don't exist either") before this module
started. The DTOs (`ConfirmEmailRequest`, `ResendEmailConfirmationRequest`) and their
FluentValidation validators were schema-agnostic and got moved, not rewritten, into
the new `Application/Features/EmailConfirmation/` feature area.

One `AuthService.RegisterAsync` call site still references a
`SaveConfirmationTokenAsync`/`SaveConfirmationTokenDbInput` pair calling the same
kind of nonexistent stored procedure — that method belongs to a separate,
currently-unwired registration flow that is out of scope for this module (Login/
Register are explicitly excluded from this task). It was left exactly as broken as
it already was, just still compiling.

## Confirm flow

1. **WebApi** (`WebApi/Endpoints/Authentication/EmailConfirmationEndpoints.cs`) binds
   `ConfirmEmailRequest { Token }`, runs it through `ValidationFilter<ConfirmEmailRequest>`
   (token required, nothing else), then calls `IEmailConfirmationService.ConfirmAsync`.
2. **Application** (`EmailConfirmationService.ConfirmAsync`): hashes the raw token
   (`ITokenHasher.Hash`) — the raw value is never used again after this line — and
   calls `IEmailConfirmationRepository.ConfirmAsync` with the hash plus the caller's
   IP and correlation id (from `IUserContext`).
3. **Infrastructure** (`EmailConfirmationRepository`) calls
   `identity.usp_EmailConfirmation_Confirm` via Dapper, one round trip.
4. **SQL Server** does everything atomically in one transaction (see "Stored
   procedures" below) and returns one result row: `ResultCode`, `UserId`,
   `OrganizationId`.
5. Application maps `ResultCode` to a `ServiceResult<ConfirmEmailResponse>` —
   `0` and `1` both become `IsConfirmed: true` with HTTP 200; anything else becomes
   a generic HTTP 200-wrapped `400` failure (see "HTTP status vs. body code" in
   `docs/TENANT_ONBOARDING.md` for the same envelope convention this module reuses).

### Transaction boundary

One stored procedure, one transaction — `identity.usp_EmailConfirmation_Confirm`
(migration 013). `SET XACT_ABORT ON` + explicit `BEGIN TRANSACTION`/`COMMIT`/
`ROLLBACK` in `TRY/CATCH` means a failure partway through rolls back everything:
there is no reachable state where the email is confirmed but the token isn't marked
used, where the token is used but the user isn't confirmed, or where another
tenant's user is affected.

### What the procedure does, in order

1. Locks the token row by `TokenHash` with `UPDLOCK, HOLDLOCK` — this is what makes
   the whole operation concurrency-safe (see "Concurrent-use protection" below).
2. Not found → `ResultCode = 2` (generic invalid).
3. Already used (`UsedAt IS NOT NULL`) → checks whether the user really is confirmed
   before trusting the token's own state; if so, `ResultCode = 1` (idempotent
   success), else `ResultCode = 2`.
4. Revoked → `ResultCode = 2`.
5. Expired (`ExpiresAt <= now`) → `ResultCode = 2`.
6. User/organization not eligible (soft-deleted, inactive, or a — should be
   impossible given the tenant-safe FK — cross-tenant mismatch) → `ResultCode = 2`.
7. User already confirmed by a concurrent request that beat this one to the lock →
   revokes this now-moot token, `ResultCode = 1`.
8. Otherwise: sets `Users.IsEmailConfirmed = 1` (+ `UpdatedAt`), sets this token's
   `UsedAt`, revokes every *other* still-active token for the same user, writes one
   `audit.AuditLogs` row, `ResultCode = 0`.

Every branch — success or failure — writes exactly one audit row before returning.

## Resend flow

1. **WebApi**: `ResendEmailConfirmationRequest { Email }` → `ValidationFilter` (email
   required, valid format) → `IEmailConfirmationService.ResendAsync`.
2. **Application**: generates a raw token (`ITokenHasher.GenerateRawToken()`) and its
   hash **unconditionally, before knowing whether it'll be used** — hashing is pure
   and local, so generating speculatively leaks nothing; the stored procedure alone
   decides whether that hash is worth persisting.
3. Calls `IEmailConfirmationRepository.ResendAsync` with the hash, expiry
   (`AuthenticationOptions.EmailConfirmationExpiryHours` from now), configured
   cooldown/hourly-cap, the caller's IP, and correlation id.
4. **SQL Server** (`identity.usp_EmailConfirmation_Resend`, migration 013) decides:
   is there an eligible unconfirmed user for this email, is a resend cooldown
   active, has the hourly cap been hit? Only when all three checks pass does it
   revoke the user's existing active tokens and insert the new one, returning
   `ResultCode = 0` plus the user's display name (for the email).
5. **Application**: if `ResultCode != 0`, the generated raw token is simply
   discarded (its hash was never persisted) and the method returns the generic
   success response immediately — no email is sent. If `ResultCode == 0`, it builds
   the confirmation link (`{ConfirmEmailBaseUrl}?token={raw token, URL-escaped}`)
   and enqueues `JobTypes.EmailConfirmation` via the existing
   `IBackgroundJobService`/`EmailConfirmationHandler` pipeline (the same one Tenant
   Onboarding already uses — nothing new built here).
6. Either way, the exact same `ApiResponse` is returned to the client.

### Resend lookup strategy

The request carries only an email, never an `OrganizationId` — per the acceptance
criteria, the client must not need to know which tenant a user belongs to.
`identity.Users.NormalizedEmail` is unique **per organization**
(`UX_Users_OrganizationId_NormalizedEmail`), not globally, so in principle the same
email could exist as the owner of more than one organization (nothing in the schema
prevents it — Option A from the task's "possible approaches," using a slug/org code,
would require the client to already know which tenant it's confirming for, which
defeats the point of a link a user clicks from their inbox).

**Decision**: the stored procedure picks the *oldest* eligible (`IsDeleted = 0`,
`IsActive = 1`, `IsEmailConfirmed = 0`, organization not deleted) match
(`ORDER BY CreatedAt ASC`), matching Option B's spirit ("send a generic response,
act on the eligible match") without literally emailing every matching account —
issuing multiple valid tokens across different organizations for the same physical
inbox would let one email confirm accounts in organizations the requester may not
control. In practice, cross-tenant duplicate emails are not expected to occur today
(nothing in the current registration flow produces them deliberately), so this is a
defensive, deterministic tie-break rather than a load-bearing feature. Documented
here rather than guessed at, per the task's explicit instruction not to assume.

### Rate limiting and cooldown

Two independent layers:

- **Transport-level, per-IP**: `RateLimiterPolicies.PublicEmailConfirmation`
  (`WebApi/Program.cs`), 10 requests/minute, applied to both endpoints. Looser than
  registration's 5/minute — confirm-email in particular is something a legitimate
  user might retry (double-click, re-opening the email).
- **Per-user, database-enforced** (resend only): `EmailConfirmationOptions`
  (`Application/Common/Options/EmailConfirmationOptions.cs`, bound from the
  `"EmailConfirmation"` config section):
  - `ResendCooldownSeconds` (default 120) — checked against `MAX(CreatedAt)` across
    **all** of the user's tokens, not just resend-issued ones; the token issued at
    registration itself starts the cooldown clock. This is deliberate: a user who
    registers and immediately mashes "resend" several times should be rate-limited
    from the very first token, not just after their first successful resend.
  - `MaxResendsPerHour` (default 5) — counts tokens created for the user in the
    trailing hour.
  - Both parameters are passed into the stored procedure from config on every call
    (never hardcoded in SQL), so they can be tuned without a migration.

Token **lifetime** intentionally is *not* duplicated into `EmailConfirmationOptions`
— it already lives in `AuthenticationOptions.EmailConfirmationExpiryHours` (used by
both Tenant Onboarding's initial token and this module's resend), so there is
exactly one setting that governs how long a token is valid, not two that could
drift out of sync.

```json
"EmailConfirmation": {
  "ResendCooldownSeconds": 120,
  "MaxResendsPerHour": 5
}
```

### Email delivery failure strategy

**Chosen approach: the existing background-job engine, used as-is (the "Preferred"
option from the task spec) — not a new outbox.** `dbo.BackgroundJobs` (migration
012) already *is* a durable, at-least-once outbox: `EnqueueAsync` persists a job row
in one call, a separate hosted service (`BackgroundJobProcessor`) picks it up and
retries with backoff on failure, entirely decoupled from the HTTP request. This
module reuses it exactly as Tenant Onboarding already does for the same job type
(`JobTypes.EmailConfirmation`) — no new email infrastructure was built.

**Known limitation**: the token-creation stored procedure call and the
`EnqueueAsync` call are two separate database round trips (the job queue insert
does not participate in the same SQL transaction as the token creation), so there
is a narrow window where token creation succeeds but enqueueing the job could fail
(e.g. a transient connection blip) — the exact same characteristic
`TenantOnboardingService`'s own confirmation-email step already has and already
documents. If `EnqueueAsync` throws, it's caught and logged
(`ILogger.LogError`, no raw token/link in the message), and the method still
returns the generic success response — per the task's explicit instruction: *do not
delete the token, log the failure safely, allow the user to resend after the
cooldown elapses* (which is exactly the self-healing path this leaves open).

## Token lifecycle

```
Created (registration or resend)
   │
   ├─▶ Confirmed  → UsedAt set, all other active tokens for the user revoked
   ├─▶ Revoked    → superseded by a newer resend, or by a later confirmation
   └─▶ Expired    → ExpiresAt elapsed with no confirm/revoke
```

`identity.EmailConfirmationTokens` already carried every column this needs before
this module started (`TokenHash`, `ExpiresAt`, `UsedAt`, `RevokedAt`,
`RevocationReason`, `OrganizationId`) — confirmed by reading migrations 003/005/009
before writing any code, per the task's explicit "do not assume schema" instruction.
**No schema changes were needed**; migration 013 adds only stored procedures and one
supporting index.

### Token hashing

Unchanged, reused as-is: `ITokenHasher.GenerateRawToken()` returns 64
cryptographically random bytes (512 bits), Base64-encoded; `Hash(string)` is
SHA-256 over the UTF-8 bytes, hex-encoded lowercase (64 characters, matching the
`CHAR(64)` `TokenHash` column). The raw token exists only in memory, from
generation to being embedded in the confirmation link — it is never compared
directly (lookups are always by hash) and never appears in a log line, an audit
row, or an API response.

### Revocation

A token's `RevocationReason` records *why* it stopped being active — `"Superseded
by confirmation"` (another token for the same user was just used), `"Superseded by
concurrent confirmation"` (this exact token lost a race to a different one),
`"Superseded by resend"` (a new token replaced it). Revoked/used/expired tokens are
never deleted — see "Cleanup" below.

## Idempotency

- **Same token confirmed twice** (including two truly concurrent requests — verified
  live, see "Concurrent-use protection"): the second confirmation returns the exact
  same success shape (`IsConfirmed: true`), with message text distinguishing "just
  confirmed" from "already confirmed" — the *response code and shape* never differ,
  only the message string, and both are HTTP 200.
- **Resend for an already-confirmed user**: `ResultCode = 1` (NotEligible) at the
  database level, generic success at the API level, **no new token row created** —
  verified live by checking `COUNT(*)` on the user's tokens before/after.
- **Resend within cooldown / over the hourly cap**: same generic success, no new
  token row, distinct internal `ResultCode`s (`2`, `3`) for audit purposes only.

## User-enumeration protection

Neither endpoint ever varies its **public** response based on account existence,
confirmation state, active/deleted status, or organization validity:

- Confirm: `ResultCode 2` covers *all* of "token not found," "expired," "revoked,"
  "used by a since-deleted user," and "user/org not eligible" — one message
  (`Authentication.InvalidToken`), one HTTP code (400-in-body/200-transport).
- Resend: `ResultCode 1/2/3` (not eligible / cooldown / rate-limited) all produce
  the identical `Authentication.ConfirmationEmailSent` response as a genuine token
  creation — verified live against a real nonexistent email, a real already-
  confirmed email, and a real cooldown hit, all returning byte-for-byte the same
  JSON shape (differing only in the language-appropriate wording, never in content).

Internally, the stored procedures **do** distinguish every case via
`FailureReason` written to `audit.AuditLogs` — operators can see exactly what
happened; the client cannot.

## Concurrent-use protection

Verified live (not just by code inspection): two genuinely simultaneous
`POST /api/auth/confirm-email` calls with the *same* valid token, fired in
parallel. Result: one returned "confirmed" (fresh), the other returned "already
confirmed" (idempotent) — both HTTP 200. Database inspection afterward confirmed
exactly one token row with `UsedAt` set once, exactly one user with
`IsEmailConfirmed = 1`, and **two** distinct audit rows (one `Succeeded = 1` with no
`FailureReason`, one `Succeeded = 1` with `FailureReason = 'TokenAlreadyUsed'`) —
the race was fully serialized by `UPDLOCK, HOLDLOCK` on the token row in
`identity.usp_EmailConfirmation_Confirm`, and both outcomes were separately
audited even though the client-facing result was uniform.

## Stored procedures (migration 013)

- **`identity.usp_EmailConfirmation_Confirm`** — see "Confirm flow" above for the
  full step list. `ResultCode`: `0` = confirmed now, `1` = idempotent success
  (already used/already confirmed), `2` = generic invalid (every other case).
- **`identity.usp_EmailConfirmation_Resend`** — see "Resend flow" above.
  `ResultCode`: `0` = token created, `1` = no eligible account, `2` = cooldown
  active, `3` = hourly cap exceeded.
- **New index**: `IX_EmailConfirmationTokens_UserId_CreatedAt` (`UserId,
  CreatedAt DESC`) — serves the cooldown/hourly-cap lookups, which filter by
  `UserId` and scan `CreatedAt` descending; the existing
  `IX_EmailConfirmationTokens_OrganizationId_UserId_ExpiresAt` index leads with
  `OrganizationId` and sorts by `ExpiresAt`, neither of which serves this access
  pattern well.

Both procedures use `SET XACT_ABORT ON` + explicit transactions, matching every
other migration in this codebase (009, 011, 012).

## Error behavior

| Situation | HTTP | Body `code` | Notes |
|---|---|---|---|
| Missing/empty token or email | 200 | 400 | FluentValidation, same convention as every other endpoint |
| Invalid/expired/revoked/ineligible token | 200 | 400 | One generic message, no distinguishing detail |
| Already confirmed (via token or via resend) | 200 | 200 | Idempotent success |
| Resend: not eligible / cooldown / rate-limited | 200 | 200 | Identical generic success — no enumeration |
| Too many requests from one IP | 429 | — | Transport-level rate limiter, not a business outcome |
| Unhandled exception | 500 | — | `GlobalExceptionHandler`, no SQL/internal detail leaked |

## Audit behavior

Every confirm attempt and every resend attempt writes exactly one
`audit.AuditLogs` row, regardless of outcome — `Action` is `EmailConfirmation.Confirm`
or `EmailConfirmation.Resend`, `Succeeded` reflects the *public* outcome (1 for
`ResultCode` 0/1 on confirm, 1 only for `ResultCode` 0 on resend), and
`FailureReason` (never shown to the client) carries the specific internal reason —
`TokenNotFound`, `TokenExpired`, `TokenRevoked`, `TokenAlreadyUsed`,
`UserOrOrganizationNotEligible`, `NoEligibleAccount`, `CooldownActive`,
`MaxResendsPerHourExceeded`, and so on. `OrganizationId`/`UserId` are `NULL` when a
lookup fails before either is known (e.g. token not found, email doesn't match any
user) — `audit.AuditLogs` already supported nullable tenant/user context before this
module (same pattern `identity.SignInLogs` uses). No raw token, token hash, or full
email is ever written to an audit row or a log line — verified by grep across the
new code (see "Security controls" in the final report).

## Current limitations

- **Email delivery is not transactionally atomic with token creation** — see "Email
  delivery failure strategy" above. Accepted, documented, and matches an existing
  precedent in this codebase (Tenant Onboarding has the same characteristic).
- **No dedicated SQL-Server-backed integration test project** was built for this
  pass — verification instead used direct `sqlcmd` execution against the real
  `Nexa` database (schema application, stored-procedure smoke tests, a genuine
  concurrent-request race test) plus mocked Application-layer unit tests. This
  matches how every other module in this codebase has been verified so far, but it
  means there is no repeatable, checked-in xUnit suite that exercises the stored
  procedures directly — only this document's record of what was run and observed.
- **Frontend confirmation page is not part of this task** — the API assumes a
  frontend reads `token` from its own query string and POSTs it; no such page
  exists yet in this repository.
- **`AuthService.RegisterAsync`'s own (separate, unwired, already-broken) token
  save path was left broken** — see "Relationship to AuthService" above.

## Future cleanup strategy

Expired/used/revoked tokens are **not** deleted by this module — they're valuable
security history (an operator investigating a suspicious login wants to see every
token a user's account ever issued). No cleanup job was built here, per the task's
explicit instruction not to add a large cleanup job unless the platform already
schedules jobs — it does now (`dbo.ScheduledJobs`, migration 012), so a future pass
could register a scheduled job that archives or purges
`identity.EmailConfirmationTokens` rows older than some retention window
(30–90 days suggested) once that becomes operationally necessary. Not built now
because there is no real data volume yet to justify it.
