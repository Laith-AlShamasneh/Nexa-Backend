# Nexa — Architecture Rules (Non-Negotiable)

These rules take precedence over convenience. A PR that violates one of these should not be merged without an explicit, documented exception.

## 1. Domain purity
- `Domain` has no ProjectReference other than, at most, `Shared` — and only if a truly generic primitive is needed. (Current exception: `Domain → Shared` for `Person.Gender` to reuse `Shared.Enums.Identity.GenderTypes` rather than duplicate it.)
- No Dapper, no ASP.NET Core, no `Microsoft.Extensions.*`, no logging, no HTTP, no configuration binding in `Domain`.
- Domain exceptions (`DomainException`, `NotFoundException`, `ForbiddenException`, `ValidationAppException`) express business-rule violations, not infrastructure failures.
- **No ORM or database mapping attributes anywhere in `Domain`** — no `[Table]`, `[Column]`, `[Key]`, `[ForeignKey]`, no Entity Framework attributes, no Dapper-specific attributes. Mapping between database columns and Domain types is Infrastructure's job (see [DOMAIN_MODEL.md](DOMAIN_MODEL.md) "Dapper construction strategy").
- **Domain entities must not expose unrestricted public setters.** Properties are `{ get; private set; }` (or get-only where the value never changes after construction); state changes only through named behavior methods that enforce invariants (`user.ChangePasswordHash(...)`, not `user.PasswordHash = ...`).
- **Dapper query DTOs do not belong in `Domain`.** Row-shaped materialization types (whatever a raw `Dapper.Query<T>()` call returns) are an Infrastructure concern; `Domain` entities are only ever built via their own `Create`/`Reconstitute` factories, never by Dapper's reflection-based mapper directly.
- **Not every database table requires a rich aggregate.** Read-only seeded catalogs (`Permission`), append-only records (`SignInLog`, `AuditLog`), and pure join facts (`UserRole`, `RolePermission`) are modeled as simply as their actual behavior warrants — see [DOMAIN_MODEL.md](DOMAIN_MODEL.md) for the classification of every table.
- **Append-only security/audit records must remain append-only.** `SignInLog` and `AuditLog` expose no `Update`/`Delete` method, ever — a future feature needing to "correct" a log entry writes a new entry, it does not mutate an old one.
- **Domain entities must not depend on full navigation graphs.** No `ICollection<Child>` / parent-reference navigation properties (`public Organization Organization { get; set; }`) anywhere — entities reference each other by Id only, since Dapper never hydrates an object graph and EF-style navigation properties would be actively misleading here.

## 2. Application independence
- `Application` depends only on `Domain` and `Shared`.
- `Application` declares contracts (`I*Repository`, `I*Service`) and consumes them — it never references a concrete `Infrastructure` type.
- No Dapper types (`DynamicParameters`, `SqlMapper.GridReader`) in any `Application` file. If a raw-DB-executor interface is needed, it belongs in `Infrastructure` (see [SOLUTION_STRUCTURE.md](SOLUTION_STRUCTURE.md) for why `IDbExecutor`/`ISqlConnectionFactory` live there, not in Application).
- No ASP.NET Core request/response types (`IFormFile`, `HttpContext`, `ClaimsPrincipal`, `HttpRequest`) in any `Application` file. Use a transport-agnostic stand-in (see `Application.Common.Upload.FileUpload`) and map at the WebApi boundary.

## 3. Infrastructure implementation boundaries
- `Infrastructure` implements Application's contracts; it does not define new contracts that Application or WebApi need to reference.
- All SQL Server access goes through stored procedures called via Dapper's `IDbExecutor` — no inline ad-hoc SQL strings, no ORM.
- `Infrastructure` may reference the ASP.NET Core shared framework (`FrameworkReference Microsoft.AspNetCore.App`) where a contract genuinely requires it (e.g. `IUserContext`'s implementation reads `HttpContext`) — but it must never define an API endpoint, controller, or route.

## 4. WebApi responsibilities
- `Program.cs` and any endpoint/controller files are the only place HTTP concerns live.
- No business orchestration in an endpoint handler beyond: validate → call Application service → map result to an HTTP response. If an endpoint accumulates `if` branches implementing business rules, that logic belongs in Application.
- No direct database access, no direct `IDbExecutor`/repository usage from WebApi — always go through an Application service.

## 5. Shared project restrictions
- `Shared` contains only: Result/error types, pagination primitives, cross-cutting enums, i18n message-key constants, and general-purpose extensions with obvious, non-business value.
- Nothing in `Shared` may reference `Domain`, `Application`, or `Infrastructure`.
- A type in `Shared` that starts accumulating vertical-specific fields (e.g. an enum that only makes sense for "Courses") must move to the owning feature slice, not stay in Shared "for convenience."

## 6. No circular dependencies
Dependency direction is strictly `WebApi → Infrastructure → Application → Domain`, with `Shared` sitting underneath `Application`/`Infrastructure`/`Domain` as a leaf. A ProjectReference that would create a cycle (e.g. `Domain → Application`) is never acceptable — restructure the code instead.

## 7. No database access outside Infrastructure
Every stored-procedure call lives in an `Infrastructure/Services/<Feature>/<Feature>Repository.cs` file implementing an Application-declared `I<Feature>Repository` interface. No other project may reference `Microsoft.Data.SqlClient` or `Dapper` directly.

## 8. No business logic in controllers/endpoints
Endpoint handlers translate HTTP ↔ Application DTOs and nothing else. Validation rules live in FluentValidation validators (Application); authorization rules live in policy/permission checks (WebApi wiring calling into Application-declared checks); business rules live in Application services.

## 9. No HTTP concepts in Domain/Application
Covered by rules 1 and 2 — restated here because it is the single most common violation to watch for in review (it already happened once with `RegisterRequest.ProfileImage : IFormFile` and was fixed).

## 10. Tenant isolation is non-negotiable
- **No `OrganizationId` accepted from untrusted request payloads for tenant-scoped operations.** A request body, query string, or route parameter must never be trusted to say which organization it belongs to.
- **Tenant context must come from the authenticated context only** — `IUserContext.OrganizationId`, resolved from the JWT's `org_id` claim (see `Infrastructure/Services/Authentication/UserContext.cs`), never from a header or body field a client controls.
- **Every tenant-scoped query/stored procedure must filter by `OrganizationId`.** No exceptions for "it's probably fine because the caller already filtered upstream."
- **Multi-tenant data leakage is a critical defect**, treated with the same severity as an authentication bypass — see [MULTI_TENANCY.md](MULTI_TENANCY.md) for the full model and [SECURITY_BASELINE.md](SECURITY_BASELINE.md) for the layered defenses.

## 11. Avoid over-engineering
- No MediatR, no CQRS pipeline, no generic repository-of-everything abstraction, no Vertical Slice Architecture — none of these are in use, and none should be introduced without an explicit decision to do so.
- Prefer a purpose-built interface per feature over a generic `IRepository<T>`.
- Three similar lines of code beat a premature abstraction.

## 12. Prefer explicit code
- Favor readable, direct implementations over clever indirection.
- A new interface needs a real second implementation or a real testing need to justify itself — "might swap providers later" is not suffient on its own if there's no concrete plan to.

## 13. Dapper and stored-procedure conventions
- Stored procedure names are schema-qualified strings matching the SQL schema they belong to (`identity.usp_Authentication_Login`, `notification.usp_Notification_Create`, `dbo.usp_BackgroundJob_Enqueue` for non-tenant system tables) — see [docs/database/DATABASE_FINAL_BLUEPRINT.md](database/DATABASE_FINAL_BLUEPRINT.md) for schema ownership.
- Parameters are built via Dapper's `DynamicParameters` inside the repository method, never assembled as raw SQL string concatenation.
- Every stored procedure call passes a `CancellationToken`.

## 14. Transaction ownership
- A repository method that must execute multiple statements atomically owns its own transaction (via the stored procedure itself, or an explicit `IDbConnection` transaction scoped to that one method) — transactions are never left open across repository calls or leaked to Application/WebApi.
- Application services orchestrate *calls* to repositories; they do not manage `IDbConnection`/transaction objects directly (those are Infrastructure concerns, hidden behind the repository interface).

## 15. Error handling
- Domain/business-rule violations throw `Domain` exceptions (`NotFoundException`, `ForbiddenException`, `ValidationAppException`) or return a `Shared.Results.ServiceResult` failure — pick one pattern per feature and stay consistent within it (Authentication currently uses `ServiceResult`).
- Infrastructure failures (DB connection errors, SMTP failures) propagate as exceptions and are caught at the WebApi boundary by the global exception handler (`GlobalExceptionHandler` in `Program.cs`), never swallowed silently.
- Never catch `Exception` and continue silently — log it, and either rethrow or return an explicit failure result.

## 16. Logging rules
- Log through `ILogger<T>`, never `Console.WriteLine`.
- Log unhandled exceptions with full context (method, relevant IDs) at `Error`; log expected failures (validation, not-found) at `Information`/`Warning`, not `Error`.
- **Security-sensitive data must never be logged**: passwords, raw tokens (refresh/reset/confirmation), full JWTs, SMTP credentials, connection strings. Log a token's *hash* or a truncated identifier if correlation is needed, never the raw value.
