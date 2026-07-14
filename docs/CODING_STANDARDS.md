# Nexa — Coding Standards

## Naming conventions

- **Namespaces** mirror folder structure exactly, rooted at the project name: `Application.Features.Authentication.Services`, `Infrastructure.Services.Storage`. No abbreviations.
- **Interfaces** are prefixed `I` and named for the capability, not the implementation: `IEmailService`, `ITokenHasher`, `ICacheService` — never `IEmailServiceBase` or `IEmailServiceImpl`.
- **Async methods** are suffixed `Async` without exception: `HandleAsync`, `QueryListAsync`, `CreateConnectionAsync`.
- **Options classes** are suffixed `Options` and live in an `Options/` subfolder next to the service that binds them: `JwtOptions`, `SmtpOptions`, `BackgroundJobOptions`.
- **DI registration methods** are named `Add<Layer>`: `AddApplication()`, `AddInfrastructure()`. One static `ServiceCollectionExtensions` class per project, in `<Project>/Common/Extensions/` (Application) or `<Project>/Extensions/` (Infrastructure).
- Avoid vague names — `Helper`, `Utility`, `Manager`, `Misc`, `CommonHelper` — unless the single responsibility is genuinely captured by that word (`LocalizationUtility` is fine because it does exactly one focused thing: extract a localized string from a JSON blob). Prefer a name that states what the type does: `StorageUtility` builds file keys/paths — if it grew unrelated responsibilities, it should split, not grow.

## File organization

- One public type per file, file name matches the type name, except tightly-coupled small DTOs that make sense grouped (`LoginDto.cs` containing both `LoginRequest` and `LoginResponse` is fine; unrelated types sharing a file is not).
- Feature slices under `Application/Features/<Name>/` always use the same four subfolders when applicable: `DTOs/`, `DbModels/`, `Services/`, `Validators/`.

## CancellationToken usage

- Every async method that can do I/O takes a `CancellationToken ct = default` as its last parameter and threads it through to every downstream call (DB, HTTP, file I/O). Never swallow or ignore an incoming token.

## Nullability

- `<Nullable>enable</Nullable>` is set on every project — keep it that way. A nullable reference type means "the caller must handle null," not "I didn't think about it."
- Prefer `required` properties (C# required members) over defensive null-checks in constructors for DTOs that must always be fully populated (see `FileUpload`).

## Immutability preferences

- DTOs that flow one direction (requests read once, responses returned once) are `sealed record`s with positional or init-only properties (see `RegisterResponse`, `OrganizationInvitationEmailPayload`).
- Mutable state (entities being built up field-by-field, e.g. `RegisterRequest` bound from a form) can be a `sealed class` with settable properties — don't force records where the type is genuinely a mutable input model.

## Record vs. class guidance

- **Record**: value-like data that flows through the system unchanged (DTOs, job payloads, options snapshots) and benefits from value equality.
- **Class**: anything with real behavior, identity beyond its data, or that's resolved from DI (`internal sealed class AuthService`, `internal sealed class JwtService`).

## Exception usage

- Domain-rule violations throw the specific `Domain` exception type (`NotFoundException`, `ForbiddenException`, `ValidationAppException`), never a bare `Exception` or `InvalidOperationException` for an expected business-rule failure.
- `InvalidOperationException` is reserved for genuine programmer-error/misconfiguration cases (e.g. `SqlConnectionFactory` throwing when the connection string is missing — a deployment/config bug, not a business-rule failure).

## Result pattern usage

- `Shared.Results.ServiceResult`/`ServiceResultFactory` is the standard return shape for Application services that can fail in an expected, user-facing way (wrong password, email already in use). Use it consistently within a feature rather than mixing exceptions and results for the same kind of failure.

## Date/time handling

- All persisted timestamps are UTC (`DateTime.UtcNow`, `SYSUTCDATETIME()` in SQL — see [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md)). Never store or compare local time.
- `DateOnly`/`TimeOnly` for values that are genuinely date-only or time-only (e.g. `DateOfBirth`), not `DateTime` with a zeroed time component.

## GUID/ID conventions

- Entity IDs are `UNIQUEIDENTIFIER` (`Guid` in C#) for anything cross-tenant-referenced or API-exposed, matching the finalized DB design. **Known exception to reconcile**: `IUserContext.UserId` and the Authentication feature's DB models currently use `long` — this predates the finalized `Guid`-based schema and is flagged as a remaining risk (see the cleanup report) rather than silently changed.
- High-volume, append-only, never-cross-referenced rows (tokens, logs) use `BIGINT IDENTITY` — see the database blueprint's rationale.

## Comments and XML documentation

- No comments that restate what the code obviously does.
- A comment is warranted for: a non-obvious invariant, a security-relevant decision (see `PasswordHasher`'s work-factor comment), a workaround for a specific constraint. `IUserContext.OrganizationId`'s doc comment is a good example — it states *why* the value must come from a JWT claim, not just what the property returns.
- XML doc comments (`///`) are used sparingly, on public interfaces where the contract isn't self-evident from the name (see `IJobHandler`, `EmailTemplateService`'s class-level summary) — not on every method of every class.

## Logging conventions

- Structured logging only: `logger.LogError(ex, "Job {JobId} ({JobType}) failed...", job.JobId, job.JobType)` — never string-interpolate values into the message template.
- Never log secrets — see [ARCHITECTURE_RULES.md](ARCHITECTURE_RULES.md) rule 16 and [SECURITY_BASELINE.md](SECURITY_BASELINE.md).
- **Serilog is the logging provider** (`builder.Host.UseSerilog()` in `WebApi/Program.cs`), writing to Console and to a dedicated `NexaLogs` database (`Serilog:WriteTo:MSSqlServer` in `appsettings.json`) — every `ILogger<T>` call in the app goes through it, no extra plumbing needed in application code.
- **Don't put a correlation/request id in the message text.** `CorrelationIdMiddleware` already pushes `CorrelationId` onto Serilog's `LogContext` for the whole request (alongside `UserId`, `OrganizationId`, `IPAddress`, `RequestPath`, `RequestMethod`) — every log line for that request gets it as its own queryable column in `NexaLogs.dbo.ApplicationLogs` automatically. Repeating it inside `LogInformation("...{CorrelationId}...", id)` is redundant and, worse, can silently drift from the actual per-request value if a different id is threaded in by hand (this happened once — see the Tenant Onboarding module's history).

## Validation conventions

- One FluentValidation validator class per request type, named `<Request>Validator`.
- Validation messages reference `Shared.Constants.MessageKeys` constants, never inline string literals, so they stay translatable via `IMessageProvider`.

## Avoiding magic strings and numbers

- Job type names, notification codes, and message keys are `const string` catalogs (`JobTypes`, `NotificationCodes`, `MessageKeys`) — never a raw string literal duplicated at each call site.
- Numeric business rules (lockout attempt count, token expiry) live in `Options` classes bound from configuration, not hardcoded in service logic.

## Avoiding static mutable state

- No `static` mutable fields for request-scoped or tenant-scoped data — ever. This is exactly the kind of bug that causes cross-tenant data leakage. Per-request state goes through DI-scoped services (`IUserContext`, scoped repositories).

## Dependency injection conventions

- Constructor injection only (primary-constructor syntax, e.g. `internal sealed class AuthRepository(IDbExecutor db) : IAuthRepository`), no service-locator pattern, no `IServiceProvider` injected into a class just to resolve something it could take as a constructor parameter — the one sanctioned exception is `BackgroundJobProcessor`, which needs `IServiceProvider` specifically to create a new DI scope per job.
- Lifetime defaults: `Scoped` for anything touching a DB connection or per-request user context; `Singleton` only for genuinely stateless or thread-safe shared state (`ICacheService`'s `MemoryCacheService`); `AddHostedService` for background workers.

## Modern C# usage

- Use primary constructors, collection expressions (`[...]`), and required members where they improve readability (already used throughout the codebase). Don't reach for a new C# feature purely because it's new — if the existing, more explicit form reads just as clearly, keep it.
