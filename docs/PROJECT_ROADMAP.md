# Nexa — Project Roadmap

## Phase 0 — Solution Cleanup and Foundation ✅ (this pass)

- Removed inherited code specific to the previous (personal finance / budgeting) project.
- Fixed project references and Clean Architecture violations (Dapper types and `IFormFile` had leaked into `Application`).
- Rewired DI composition (`AddApplication()`, `AddInfrastructure()`) and `WebApi/Program.cs` (health check, OpenAPI, global exception handler).
- Established the conventions and rules recorded in this `/docs` folder.
- Solution builds with zero errors/warnings; `WebApi` starts and serves `/health` and interactive API docs at `/swagger` (Development only).

## Phase 1 — Platform Foundation

- Tenant context (`IUserContext.OrganizationId`) wired end-to-end from a real JWT, not just the interface shape that exists today.
- Current-user context fully wired (currently `UserContext` reads claims that nothing yet issues).
- SQL connection factory / transaction management exercised against a real database (currently untested against a live SQL Server instance).
- Result/error handling pattern (`ServiceResult`) applied consistently as new features land.
- Structured logging conventions enforced (see [CODING_STANDARDS.md](CODING_STANDARDS.md)).
- Global exception handling and ProblemDetails responses (baseline exists in `Program.cs`; extend with domain-exception-to-status-code mapping once endpoints exist).
- Validation wiring confirmed against real endpoints (FluentValidation registration exists; nothing calls it yet).
- Audit-logging foundation: nothing writes to `audit.AuditLogs` yet — build the write path.

## Phase 2 — Tenant Onboarding ✅ (active/completed — this pass)

- ✅ Organization creation — one atomic transaction (`tenant.usp_Organization_Register`, migration 011) via `POST /api/organizations/register`. See [docs/TENANT_ONBOARDING.md](TENANT_ONBOARDING.md).
- ✅ Main branch creation.
- ✅ Organization settings (created and populated from the request's time zone/language/currency).
- ✅ Owner person + owner user creation.
- ✅ Tenant roles cloned from the four global system templates (set-based `INSERT ... SELECT`, not a per-signup C# loop).
- ✅ Role permissions cloned alongside each tenant role.
- ✅ Owner role assignment (to the tenant-local clone, never the global template).
- ✅ Email confirmation token issuance (hash only persisted; raw token handed to the existing email pipeline).
- ✅ Audit entries (six rows per registration: Organization/Branch/User created, Roles initialized, UserRoles assigned, EmailConfirmationTokens created).
- ✅ Public-endpoint rate limiting (5 req/min/IP).
- ✅ Unit tests (Application layer — password hashing, token hash-only persistence, conflict/missing-template mapping, validator rules).
- ⏳ **Not done in this pass**: Confirm Email endpoint (see Phase 3), integration tests against a real SQL Server test fixture, stronger anti-abuse controls (CAPTCHA, device fingerprinting).

## Phase 3 — Identity and Authentication

- ✅ **Email Confirmation** — `POST /api/auth/confirm-email` and `POST /api/auth/resend-email-confirmation`, fully implemented end-to-end (database migration 013, Application `IEmailConfirmationService`, Infrastructure repository, WebApi endpoints, rate limiting, audit logging). Atomic, idempotent, concurrency-safe (verified live), enumeration-safe. See [docs/EMAIL_CONFIRMATION.md](EMAIL_CONFIRMATION.md). The old `AuthService.ConfirmEmailAsync`/`ResendConfirmationEmailAsync` (which called nonexistent stored procedures and were never wired to any endpoint) were removed in favor of this dedicated module.
- Login. **Recommended next module** — see [docs/EMAIL_CONFIRMATION.md](EMAIL_CONFIRMATION.md) "Next recommended module" for why.
- Refresh-token rotation (schema supports reuse detection via `TokenFamilyId`; confirm the detection logic is actually implemented, not just the columns).
- Logout.
- Session revocation (`identity.UserSessions` table exists; no service writes to it yet).
- Forgot password / reset password.
- Lockout (columns + options exist; confirm the increment/check logic).
- Sign-in logs (table exists; nothing writes to it yet).

## Phase 4 — User and Role Administration

- Invitations (`identity.UserInvitations` table + `OrganizationInvitationEmailPayload`/`OrganizationInvitationEmailHandler` exist; no service/endpoint to create an invitation yet).
- Employee accounts.
- Role assignment (tenant-local role model exists per migration 009).
- Permission checks (permission catalog exists; enforcement wiring doesn't).
- User activation/deactivation.
- Session administration (revoke one session, revoke all sessions).

## Phase 5 — CRM Foundation

- Customers (`crm.Customers` table exists — generic, `CustomerType` string field distinguishes "Student" from future verticals' "Patient"/"Client").
- Customer search.
- Customer profile.
- Customer notes (`crm.CustomerNotes` table exists, full soft-delete-with-actor per migration 009).
- Soft delete and restore.

## Phase 6 — Service and Education Foundation

- Generic `Service`/`education.Courses` concept (per [database blueprint §11](database/DATABASE_FINAL_BLUEPRINT.md) — not modeled yet, `education` schema exists empty).
- Courses.
- Batches/classes.
- Enrollments (junction between `crm.Customers` and `education.Classes`).
- Teachers (a `Users` row holding a `Teacher` role, assigned to a class).

## Phase 7 — Billing

- Pricing.
- Payment plans.
- Installments.
- Payments.
- Receipts.
- Outstanding balances.

## Phase 8 — Attendance

- Class sessions.
- Attendance records.
- Attendance reporting.
- At-risk-student indicators.

## Phase 9 — Communication

- Templates (an `EmailTemplateService` already exists generically — extend its template set, don't rebuild it).
- Notification history (`Notifications` feature already exists generically — extend, don't rebuild).
- Email (SMTP sending already exists generically).
- Manual WhatsApp flow.
- Automated reminders (later).

## Phase 10 — Dashboard

- Owner KPIs.
- Revenue.
- Outstanding payments.
- Active students.
- Attendance.
- Course performance.

## Explicitly deferred (not on this roadmap yet)

- Mobile application
- Parent portal
- Student portal
- Online learning
- Exams
- Certificates
- Full accounting
- Inventory
- AI features
- WhatsApp Business API (beyond the manual-link flow in Phase 9)
- Additional business verticals (clinics, beauty centers) — the Core is built to support them later ([PRODUCT_CONTEXT.md](../PRODUCT_CONTEXT.md), [database blueprint §11](database/DATABASE_FINAL_BLUEPRINT.md)), but no vertical-specific work for them is scheduled

## A note on "exists" vs. "done"

Several building blocks from the previous (finance-app) codebase were generic enough to keep — a full authentication service, a background-job engine, a notification engine, an onboarding-wizard engine, email/storage/caching infrastructure. These are real, compiling code, not stubs. But **most of it is still not connected to a running database or a real HTTP endpoint** (Phase 0 deliberately stopped short of that — see the cleanup report). Phases 1–3 above are largely about connecting existing scaffolding to real stored procedures and real endpoints, not writing everything from scratch.

**Exception**: the background-job engine (`dbo.BackgroundJobs`) and the new recurring-job engine (`dbo.ScheduledJobs`) are both real and connected end-to-end — database (migration 012), Application contracts, Infrastructure repositories, and hosted services (`BackgroundJobProcessor`, `ScheduledJobProcessor`) all verified live against a running WebApi instance. See [docs/BACKGROUND_JOBS.md](BACKGROUND_JOBS.md). What's still missing: any actual `IJobHandler` for a recurring job, and an admin-facing way to create `ScheduledJobs` entries outside raw SQL.
