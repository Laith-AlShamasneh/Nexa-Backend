# Nexa — Solution Structure

## Overview

Nexa is a multi-tenant SaaS platform (education institutes first, other service verticals later — see [PRODUCT_CONTEXT.md](../PRODUCT_CONTEXT.md)). The backend is a practical Clean Architecture split across five projects:

```
Domain          — entities, value objects, domain exceptions, business rules. No dependencies.
Application     — use cases, contracts (interfaces), DTOs, validators. Depends on Domain + Shared.
Infrastructure  — Dapper/SQL Server, JWT, email, storage, caching, background jobs. Implements Application's contracts.
Shared          — small, genuinely cross-cutting primitives (Result types, enums, constants). No business-project dependency.
WebApi          — composition root: Program.cs, DI wiring, middleware, Swagger/OpenAPI, health checks.
```

## Dependency Diagram

```text
        Domain
          ↑
       Shared  (no dependency on Domain; both are leaves)
          ↑
      Application  ──depends on──▶  Domain, Shared
          ↑
     Infrastructure ──depends on──▶  Application (and transitively Domain, Shared)
          ↑
        WebApi     ──depends on──▶  Application, Infrastructure
```

Infrastructure **implements** the interfaces Application declares (`IAuthRepository`, `IEmailService`, `ICacheService`, ...); it never introduces new public contracts of its own that Application or WebApi need to know about. WebApi **composes** the application: it calls `AddApplication()` and `AddInfrastructure()` in `Program.cs`, wires up cross-cutting HTTP concerns (exception handling, OpenAPI, health checks), and otherwise contains no business logic.

## Project Responsibilities

### Domain
Entities, value objects, domain enums/constants, domain exceptions (`NotFoundException`, `ForbiddenException`, `ValidationAppException`, `DomainException`), invariants. Currently intentionally thin — the education/CRM/billing entities described in [docs/database/](database/DATABASE_FINAL_BLUEPRINT.md) will land here as each vertical is implemented (Phase 5+ of [PROJECT_ROADMAP.md](PROJECT_ROADMAP.md)).

**Never**: a project reference to Application, Infrastructure, Shared (unless truly justified), WebApi, Dapper, ASP.NET Core, HTTP types, logging frameworks, or configuration systems.

### Application
Organized by **feature slice** under `Application/Features/<FeatureName>/{DTOs,DbModels,Services,Validators}`, plus cross-cutting contracts under `Application/Interfaces/{Repositories,Services,Jobs}` and cross-cutting building blocks under `Application/Common/{Constants,Options,Upload,Extensions}`.

Current feature slices: `Authentication`, `Notifications`, `Onboarding`, `BackgroundJobs`, `Email` (job payloads only). Each new business feature (Customers, Payments, Attendance, ...) gets its own `Features/<Name>` folder following the same shape.

**Allowed**: use-case services, DTOs, FluentValidation validators, repository/service *interfaces*, mapping logic, Result/error wrapping.
**Never**: Dapper types (`DynamicParameters`, `SqlMapper`), `Microsoft.AspNetCore.Http` types (`IFormFile`, `HttpContext`), SQL, or any concrete Infrastructure class.

### Infrastructure
Organized by concern: `Database/` (Dapper executor + connection factory), `Services/<Concern>/` (Authentication, Caching, Email, Localization, Notifications, Onboarding, Storage), `Jobs/` (the background-job engine + `Handlers/`), and `Extensions/ServiceCollectionExtensions.cs` (the `AddInfrastructure()` composition entry point).

**Allowed**: implementations of Application interfaces, SQL/stored-procedure calls, JWT/password/token mechanics, email/file/cache mechanics, DI registration.
**Never**: API controllers or endpoint definitions, business orchestration that belongs in an Application service, domain rules.

### Shared
Deliberately tiny: `Constants/MessageKeys.cs` (i18n key catalog), `Enums/` (cross-cutting technical enums — Identity, Jobs, Notifications, System), `Responses/` (`ApiResponse`, `PagedResponse`), `Results/` (`ServiceResult`, `ServiceResultFactory`), `Utilities/LocalizationUtility.cs`.

Every file here must be genuinely reusable across *any* future vertical (education, clinic, beauty center) and must not encode business rules. If a "shared" file starts accumulating vertical-specific fields, that's a sign it should move to a feature slice in Application instead.

### WebApi
`Program.cs` is the entire composition root today: `AddApplication()`, `AddInfrastructure(configuration)`, OpenAPI, health checks, and a single global exception handler. As real endpoints are added, they go here (or in a `Controllers/`/`Endpoints/` folder) — never in Infrastructure or Application.

## Folder Conventions — Where New Code Goes

| You're adding... | Goes in |
|---|---|
| A new business entity (e.g. `Customer`) | `Domain/Entities/Customer.cs` |
| A use-case service for that entity | `Application/Features/Customers/Services/CustomerService.cs` |
| Its repository contract | `Application/Interfaces/Repositories/ICustomerRepository.cs` |
| Its repository implementation (Dapper) | `Infrastructure/Services/Customers/CustomerRepository.cs` |
| Its request/response DTOs | `Application/Features/Customers/DTOs/CustomerDtos.cs` |
| Its FluentValidation rules | `Application/Features/Customers/Validators/CreateCustomerValidator.cs` |
| Its API endpoint | `WebApi/Endpoints/Customers/*.cs` (or a controller, once the endpoint style is chosen) |
| A new cross-cutting enum used by 2+ verticals | `Shared/Enums/<Category>/` |
| A one-off enum only `Customers` cares about | `Domain/Enums/` (or inline in the feature) — **not** Shared |

### Correct placement — examples
- `IUserContext` (contract) in `Application/Interfaces/Services/`, its HTTP-backed implementation `UserContext` in `Infrastructure/Services/Authentication/` — contract and implementation on opposite sides of the Application/Infrastructure boundary. ✅
- `NotificationCategory` enum in `Shared/Enums/Notifications/` — used by both Application (service logic) and Infrastructure (repository mapping), genuinely cross-cutting. ✅

### Incorrect placement — examples (already fixed once; don't reintroduce)
- `IDbExecutor`/`ISqlConnectionFactory` living in `Application/Interfaces/Database/` — they leaked Dapper's `DynamicParameters`/`SqlMapper` types into Application and were only ever consumed by Infrastructure repositories. They now live in `Infrastructure/Database/` alongside their implementations. ❌ → don't add another Dapper-shaped interface to Application.
- `RegisterRequest.ProfileImage` typed as `Microsoft.AspNetCore.Http.IFormFile` — an ASP.NET Core request type inside an Application DTO. Fixed by introducing `Application.Common.Upload.FileUpload`, a transport-agnostic stand-in that WebApi maps `IFormFile` into. ❌ → don't reach for `IFormFile`, `HttpContext`, or `ClaimsPrincipal` anywhere under `Application/`.

## Practical Clean Architecture — how it's applied here

This is "Clean Architecture with practical modifications," not textbook CQRS/MediatR/Vertical-Slice. Concretely:
- No MediatR — Application services are called directly (once WebApi has real endpoints, they'll inject the service interface and call it).
- No generic repository abstraction over Dapper — each feature gets a purpose-built repository interface shaped around what that feature actually needs, backed by stored procedures.
- Stored procedures are the only way Infrastructure talks to SQL Server — no inline SQL, no ORM.
- Multi-tenancy is a cross-cutting concern threaded through `IUserContext.OrganizationId`, not a separate architectural layer — see [MULTI_TENANCY.md](MULTI_TENANCY.md).
