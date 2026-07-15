# Nexa — Background Jobs and Scheduled Jobs

Database design for `dbo.BackgroundJobs` and `dbo.ScheduledJobs`, added in
[`Database/Migrations/012_BackgroundJobs_And_Scheduling.sql`](../Database/Migrations/012_BackgroundJobs_And_Scheduling.sql).
This is the **database side only** — see "C# work still needed" below for
what Infrastructure/Application must still do to wire it up.

## Why `dbo`, not a tenant schema

Both tables are system/operational infrastructure, not tenant business data
— they live alongside `dbo.SchemaVersions` rather than under `tenant`,
`identity`, etc. `OrganizationId` is present but **nullable** on both
tables: most jobs are enqueued with their tenant context embedded in the
JSON `Payload` (matching the existing `BackgroundJobEnqueueInput` contract,
which has no `OrganizationId` parameter today), while the column itself
exists so operational tooling can later filter/cancel "all jobs for
organization X" without parsing JSON.

## dbo.BackgroundJobs — fire-and-forget work queue

Ported from MyMoney's proven `BackgroundJobs` table and four stored
procedures (dedup-aware enqueue, atomic claim, exponential-backoff retry),
with these deliberate changes:

| Aspect | MyMoney | Nexa |
|---|---|---|
| Actor columns (`CreatedBy`) | `BIGINT` | `UNIQUEIDENTIFIER` (matches `identity.Users.Id`) |
| Timestamp precision | `DATETIME2(0)` / `GETUTCDATE()` | `DATETIME2(3)` / `SYSUTCDATETIME()`, matching every other Nexa table |
| Tenant awareness | None (single-tenant) | Optional `OrganizationId` column, informational only |
| Dedup support | Added later via a follow-up migration | Present from the start |

Lifecycle: `Enqueue` (StatusId=1 Pending) → `PickUp` (StatusId=2
Processing, atomic `UPDLOCK, READPAST` claim ordered by `Priority` then
`ScheduledAt`) → `Complete` (StatusId=3) or `Fail` (StatusId=4, with
`NextRetryAt = now + 2^AttemptCount minutes` if `AttemptCount < MaxAttempts`,
else the job is exhausted and stays Failed with `NextRetryAt = NULL`).
`PickUp` also reclaims Failed jobs whose `NextRetryAt` has passed.

Stored procedures — signatures match what
[`Infrastructure/Jobs/BackgroundJobRepository.cs`](../Infrastructure/Jobs/BackgroundJobRepository.cs)
already calls:

- `dbo.usp_BackgroundJob_Enqueue` — catches unique-violation errors 2601/2627
  on `UX_BackgroundJobs_DedupKey_Active` and returns the existing job's id
  instead of raising, when `@DedupKey` collides with an active (Pending or
  Failed) job. **Deliberately does not `SET XACT_ABORT ON`** — that setting
  would doom the caller's ambient transaction the instant the constraint
  violation occurs, defeating the catch-and-recover dedup path. Every other
  procedure in this migration does set it.
- `dbo.usp_BackgroundJob_PickUp` — claims up to `@BatchSize` due jobs.
- `dbo.usp_BackgroundJob_Complete`, `dbo.usp_BackgroundJob_Fail`.

## dbo.ScheduledJobs — recurring-job definitions (new)

MyMoney had no equivalent: recurring behavior was hardcoded ASP.NET Core
hosted-service timers calling feature-specific procs directly. That has a
real correctness gap — scale the API past one instance and every instance's
timer fires independently, running the same recurring action multiple times
(duplicate emails, duplicate reports, double-charged jobs).

`ScheduledJobs` stores the recurring-job *definition* (either a
`CronExpression` or a plain `IntervalSeconds` — exactly one of the two is
required, enforced by `CK_ScheduledJobs_ScheduleSource`) and is claimed
through the same atomic `UPDLOCK, READPAST` pattern as `BackgroundJobs`
pickup, so only one instance ever claims a given due schedule at a time.

Cron/interval-to-next-run computation is deliberately left to the C# caller
(a hand-rolled cron parser in T-SQL is fragile and hard to verify) — the
stored procedures only handle the atomic claim and the atomic completion.

Lifecycle: `Create` → `ClaimDue` (marks `IsClaimed=1`, `ClaimedAt=now`; a
claim older than `@ClaimTimeoutSeconds` — default 300 — is treated as
abandoned by a crashed instance and is reclaimable, mirroring
`BackgroundJobs`' own retry-after-timeout safety net) → caller computes the
next occurrence and calls the existing `usp_BackgroundJob_Enqueue` to
actually produce the job → `CompleteRun` (releases the claim, sets
`LastRunAt`, advances `NextRunAt`, records `LastEnqueuedJobId`).
`SetEnabled` toggles a schedule on/off without deleting its definition.

No general CRUD (update-all-fields, delete, paginated list) exists yet —
only what the processing engine needs plus the minimum to seed/manage
entries (`Create`, `SetEnabled`). Add the rest when an admin-facing feature
for managing schedules is actually built, not preemptively.

## C# implementation (Application + Infrastructure)

- `Application/Features/BackgroundJobs/DbModels/BackgroundJobDbModels.cs` —
  `BackgroundJobEnqueueInput.CreatedBy` is now `Guid?` (was `long?`, which
  didn't match the `UNIQUEIDENTIFIER` column and would have failed at
  runtime). Added `OrganizationId Guid?` (informational, see above). Added
  `BackgroundJobEnqueueResult` to map the proc's `(ResultCode, JobId)` row.
- `Application/Interfaces/Repositories/IBackgroundJobRepository.cs` —
  `EnqueueAsync` now returns `Task<long?>` (the enqueued or dedup-matched
  JobId) instead of `Task`.
- `Infrastructure/Jobs/BackgroundJobRepository.cs` — updated to the above;
  `@CreatedBy`/`@OrganizationId` now bound as `DbType.Guid`.
- `Infrastructure/Jobs/BackgroundJobService.cs` — passes
  `OrganizationId = userContext.OrganizationId` (real `Guid?`, already
  correctly typed) but **`CreatedBy = null`** — `IUserContext.UserId` is
  still `long` (see README "Current limitations"), so it cannot be mapped
  to the `Guid` column yet. Fix together when the identity long/Guid
  reconciliation lands.
- `Application/Features/ScheduledJobs/DbModels/ScheduledJobDbModels.cs`,
  `Application/Interfaces/Repositories/IScheduledJobRepository.cs`,
  `Infrastructure/Jobs/ScheduledJobRepository.cs` — new, one method per
  stored procedure (`Create`/`ClaimDue`/`CompleteRun`/`SetEnabled`).
- `Infrastructure/Jobs/ScheduledJobProcessor.cs` — new `BackgroundService`,
  same shape as `BackgroundJobProcessor`: polls `ClaimDueAsync` on
  `BackgroundJobOptions.PollingIntervalSeconds`, computes each schedule's
  next occurrence (`Cronos` for `CronExpression`, `DateTime.UtcNow.AddSeconds`
  for `IntervalSeconds`), enqueues via `IBackgroundJobRepository`, then
  releases the claim via `CompleteRunAsync`. Runs sequentially per batch
  (not parallelized like job execution) — triggering a schedule is cheap;
  there's no slow handler work to parallelize. On any exception the claim
  is deliberately left in place so `ClaimDue`'s timeout window reclaims it
  on a later poll, mirroring `BackgroundJobs`' own retry-after-abandonment
  behavior.
- `Infrastructure/Extensions/ServiceCollectionExtensions.cs` — registers
  `IScheduledJobRepository`; only registers `ScheduledJobProcessor` as a
  hosted service when `BackgroundJobOptions.RunSchedulers` is true (reads
  the raw config section directly, since hosted-service registration
  happens before the options pipeline is available).
- Added the `Cronos` NuGet package to `Infrastructure.csproj` for
  `CronExpression`-based schedules.

## Verified

Applied to the `Nexa` database on `localhost\SQLEXPRESS`. Two rounds of
testing:

1. **Direct SQL**, all 8 procedures: enqueue, dedup-collision-returns-same-id,
   pickup-claims-once (a second immediate pickup returns nothing), fail with
   backoff, complete, scheduled-job create/claim/claim-again-returns-nothing/
   complete-run/set-enabled.
2. **Live, end-to-end through the running WebApi** (temporarily pointed at
   `Nexa` via a `ConnectionStrings__SqlConnection` environment override —
   no file changes; the app's own configured connection string was left
   untouched): seeded a `ScheduledJobs` row with a 5-second interval and no
   registered handler. Confirmed `ScheduledJobProcessor` claimed it,
   correctly advanced `NextRunAt` by 5 seconds each cycle, and enqueued a
   `BackgroundJobs` row each time (`LastEnqueuedJobId` tracked correctly);
   confirmed `BackgroundJobProcessor` picked each one up and correctly
   failed it with "No handler registered for job type" (the expected
   outcome for a job type with no `IJobHandler` registered) — end-to-end
   confirmation of both processors working together, not just the SQL
   layer. All test rows deleted afterward; solution rebuilt clean (0
   warnings/errors).

## Still not done

- No `IJobHandler` exists yet for any recurring job — `ScheduledJobs` has
  no real consumers registered. The processor and schema are ready for the
  first one.
- No admin-facing endpoint/service to create or manage `ScheduledJobs`
  entries — only the raw repository exists. `usp_ScheduledJob_Create` must
  currently be called by seed/migration SQL or a future admin feature.
- `CreatedBy` on both tables stays `null` from all current C# call sites
  until the `IUserContext.UserId` long/Guid reconciliation happens.
