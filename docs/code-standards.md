---
type: code-standards
description: TeamFlow coding standards — naming, patterns, testing, and non-negotiable rules (extracted from CLAUDE.md)
---

# Code Standards

Quick reference for development conventions. Full detail and examples are in `CLAUDE.md`.

---

## Non-Negotiable Rules

1. Controllers call `Sender.Send()` only — no direct service injection in controllers
2. All handlers return `Result<T>` or `Result` (CSharpFunctionalExtensions)
3. No business logic in Infrastructure — only persistence and external calls
4. No business logic in Controllers — only HTTP mapping
5. Each feature in its own slice folder — no cross-slice handler imports
6. Permission check first in every command handler that modifies data
7. `WorkItemHistories` is append-only — no UPDATE or DELETE, ever
8. All API errors return `ProblemDetails` (RFC 7807) — never plain strings
9. All new classes must be `sealed` by default unless designed for inheritance
10. Test-First Development — write failing tests before implementation
11. E2E tests required for every user-facing feature — Playwright specs under `e2e/`
12. Testcontainers required for all integration tests — no SQLite or in-memory EF Core

---

## Naming Conventions

| Target | Convention | Example |
|---|---|---|
| Public types, methods, properties, constants, records | PascalCase | `CreateWorkItemHandler` |
| Locals, parameters | camelCase | `workItemId` |
| Private fields | `_camelCase` | `_repository` |
| Interfaces | `I` prefix | `IWorkItemRepository` |
| Async methods | `Async` suffix | `GetByIdAsync` |
| Commands | `{Action}{Entity}Command` | `CreateWorkItemCommand` |
| Queries | `{Action}{Entity}Query` | `GetWorkItemQuery` |
| Handlers | `{Feature}Handler` | `CreateWorkItemHandler` |
| Validators | `{Feature}Validator` | `CreateWorkItemValidator` |
| DTOs | `{Entity}Dto` | `WorkItemDto` |

---

## Sealed Classes

All new classes are `sealed` unless they serve an inheritance purpose.

```csharp
// Correct
public sealed class CreateWorkItemHandler : IRequestHandler<...> { }
public sealed record WorkItemDto(...);

// Wrong — no justification for unsealed
public class WorkItemValidator { }
```

Allowed unsealed: domain entities with inheritance (e.g., `AuditableEntity`), abstract base classes, framework extensibility points, test helper classes.

---

## CQRS Handler Pattern

```csharp
public sealed class CreateWorkItemHandler(
    IWorkItemRepository repository,
    IPermissionChecker permissions,
    ICurrentUser currentUser,
    IPublisher publisher)
    : IRequestHandler<CreateWorkItemCommand, Result<WorkItemDto>>
{
    public async Task<Result<WorkItemDto>> Handle(
        CreateWorkItemCommand request, CancellationToken ct)
    {
        // 1. Permission check — always first
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_Create, ct))
            return Result.Failure<WorkItemDto>("Forbidden: insufficient permission");

        // 2. Validate business rules
        // 3. Create entity
        // 4. Persist
        // 5. Publish domain event
        // 6. Return Result.Success(dto)
    }
}
```

---

## Controller Pattern

```csharp
[ApiVersion("1.0")]
public sealed class WorkItemsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkItemCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }
}
```

All actions annotate `[ProducesResponseType]` for Swagger accuracy. Mutation endpoints apply `[EnableRateLimiting(RateLimitPolicies.Write)]`.

---

## Result Pattern

Return `Result<T>` instead of throwing exceptions for business errors.

```csharp
var item = await repository.GetByIdAsync(id, ct);
if (item is null)
    return Result.Failure<WorkItemDto>("NotFound: work item not found");

return Result.Success(new WorkItemDto(...));
```

Error string prefixes drive HTTP status code mapping in `ApiControllerBase.HandleResult`:

| Prefix | Status |
|---|---|
| `NotFound` | 404 |
| `Forbidden` | 403 |
| `Conflict` | 409 |
| *(other)* | 400 |

---

## FluentValidation

```csharp
public sealed class CreateWorkItemValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
    }
}
```

`ValidationBehavior` runs validators before the handler. Validation failures short-circuit with a 400 response.

---

## Permission Checks

```csharp
if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_Create, ct))
    return Result.Failure<WorkItemDto>("Forbidden: insufficient permission");
```

Never write permission logic in controllers, services, or repositories. All checks go through `IPermissionChecker`. Resolution order: Individual → Team → Organization.

---

## Authentication

`IAuthService` in Infrastructure handles JWT generation, refresh token generation, token hashing, and password hashing/verification. Human reviews all changes to auth code — do not modify JWT generation or validation logic without review. See `CLAUDE.md` for the full restriction list.

JWT: HMAC-SHA256, 30-minute expiry. Refresh tokens: 64 random bytes, stored as SHA-256 hash. Passwords: BCrypt work factor 12.

---

## Test-First Development (TFD)

1. Write the failing test
2. Write the minimum code to make it pass
3. Refactor while green

**Framework:** xUnit + FluentAssertions + NSubstitute
**Integration tests:** Testcontainers with real PostgreSQL (no SQLite or in-memory EF Core)
**E2E tests:** Playwright — required for every user-facing feature; specs live under `src/apps/teamflow-web/e2e/`
**Coverage target:** ≥70% on Application layer

---

## Testing Conventions

Use `[Theory]` to eliminate duplication across similar cases:

```csharp
[Theory]
[InlineData("")]
[InlineData(null)]
public async Task Handle_InvalidTitle_ReturnsValidationError(string? title) { ... }
```

Never write a separate `[Fact]` for each invalid value.

Other rules:
- Do not add "Arrange", "Act", "Assert" comments
- Do not use magic numbers — use named constants
- Use test data builders, not raw object construction

```csharp
var project = ProjectBuilder.New()
    .WithOrganization(orgId)
    .WithStatus(ProjectStatus.Active)
    .Build();
```

---

## Test Structure

Each handler test class covers three dimensions:

```csharp
public sealed class CreateWorkItemTests : IntegrationTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesItem() { }        // Happy path

    [Fact]
    public async Task Handle_MissingTitle_ReturnsValidationError() { }  // Validation

    [Fact]
    public async Task Handle_ViewerRole_ReturnsForbidden() { }     // Permission boundary
}
```

---

## EF Core — Read vs Write

```csharp
// Read queries — always use AsNoTracking + projection
var items = await dbContext.WorkItems
    .AsNoTracking()
    .Where(w => w.ProjectId == projectId)
    .Select(w => new WorkItemDto { Id = w.Id, Title = w.Title })
    .ToListAsync(ct);

// Write operations — load tracked entity, modify, SaveChangesAsync
```

---

## Async Patterns

- `CancellationToken` on all public async methods
- No `.Result` or `.Wait()` — always `await`
- `ConfigureAwait(false)` in library code only, not needed in ASP.NET Core

---

## Logging

```csharp
// Structured logging — message templates, not string interpolation
_logger.LogInformation("Processing work item {WorkItemId} for project {ProjectId}",
    workItemId, projectId);

// Always include the exception object
_logger.LogError(ex, "Failed to create work item for project {ProjectId}", projectId);
```

Never log passwords, tokens, or PII.

---

## Message Contract Immutability

Once a domain event class is published to RabbitMQ, its namespace and class name are permanent.

Allowed: add new optional properties, add new event classes, refactor consumer logic.
Prohibited: rename class, change namespace, remove or rename properties, change property types, make optional properties required.

Breaking change protocol: create `WorkItemCreatedDomainEventV2`, keep old class, emit both during migration, migrate all consumers, deprecate old version after 3 months.

---

## API Conventions

- Route: `api/v{version}/[controller]`
- Default version: `1.0`
- Pagination: `?page=1&pageSize=20` → `{ items, totalCount, page, pageSize }`
- Dates: ISO 8601 UTC
- IDs: UUID v4 as strings

---

## Cross-References

- Architecture rules and full examples: `CLAUDE.md`
- Architecture overview: `docs/architecture/codebase-architecture.md`
- Phase scope and acceptance criteria: `docs/process/phases.md`
- Definition of done: `docs/process/definition-of-done.md`
