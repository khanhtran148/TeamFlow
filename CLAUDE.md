# CLAUDE.md — TeamFlow

> This file provides context and conventions for Claude Code when working on the TeamFlow codebase.
> Update this file whenever a new pattern is established or a convention changes.
> Every PR introducing a new architectural pattern must update this file.

---

## Project Overview

TeamFlow is an internal project management platform for engineering teams of 9–15 people.

- **API:** .NET 10, Controller-based, Clean Architecture + Vertical Slice
- **Frontend:** Next.js (App Router), TanStack Query, Zustand
- **Database:** PostgreSQL via EF Core + Npgsql
- **Messaging:** RabbitMQ via MassTransit
- **Realtime:** SignalR
- **Background Jobs:** .NET Hosted Services + Quartz.NET
- **Auth:** JWT + Refresh Token

Full documentation: see `docs/doc-index.md`.

---

## Solution Structure

```
TeamFlow.slnx
├── src/
│   ├── core/                              # /Core solution folder
│   │   ├── TeamFlow.Domain/               # Entities, Value Objects, Enums, Domain Events
│   │   ├── TeamFlow.Application/          # Vertical slices, MediatR, CQRS, Validators
│   │   └── TeamFlow.Infrastructure/       # EF Core, PostgreSQL, RabbitMQ, JWT
│   └── apps/                              # /Apps solution folder
│       ├── TeamFlow.Api/                  # Controllers, Middleware, SignalR Hub
│       ├── TeamFlow.BackgroundServices/   # Hosted services, Quartz jobs, consumers
│       └── teamflow-web/                  # Next.js frontend app
└── tests/
    ├── TeamFlow.Tests.Common/             # Shared builders, IntegrationTestBase
    ├── TeamFlow.Domain.Tests/             # Domain entity and enum tests
    ├── TeamFlow.Application.Tests/        # Handler, validator, behavior tests
    ├── TeamFlow.Infrastructure.Tests/     # EF Core, repository integration tests
    └── TeamFlow.Api.Tests/                # Controller, middleware, API integration tests
```

---

## Architecture Rules — Non-Negotiable

1. **Controllers only call `Sender.Send()`** — no direct service injection in controllers
2. **All handlers return `Result<T>` or `Result`** from CSharpFunctionalExtensions
3. **No business logic in Infrastructure layer** — only persistence and external service calls
4. **No business logic in Controllers** — only HTTP mapping
5. **Each feature in its own slice folder** — no cross-slice imports at the handler level
6. **Permission check in every command handler that modifies data** — not in controllers
7. **WorkItemHistories is append-only** — no UPDATE or DELETE ever written against this table
8. **All API errors return ProblemDetails** (RFC 7807) — never plain strings
9. **All new classes MUST be sealed by default** — unless explicitly designed for inheritance
10. **Test-First Development** — write failing tests first, then implement minimal code, then refactor while green

---

## C# Development Standards

### Language & Framework

- **Target Framework:** `net10.0`
- **C# Version:** Latest stable (C# 14 features encouraged)
- **Nullable Reference Types:** Enabled in all projects, treat warnings as errors in CI

### Naming Conventions

- **PascalCase:** public types, methods, properties, constants, records
- **camelCase:** locals, parameters
- **_camelCase:** private fields (prefix with `_`)
- **Interfaces:** prefix with `I` (e.g., `IUserService`)
- **Async methods:** end with `Async`

### Formatting Rules

- File-scoped namespace declarations
- Single-line using directives
- Newline before opening curly brace of code blocks
- Use pattern matching and switch expressions
- Use `nameof` instead of string literals
- Apply `.editorconfig` rules when available

### Class Sealing (Non-Negotiable)

All new classes MUST be sealed by default unless explicitly designed for inheritance:

```csharp
// ✅ CORRECT — Sealed by default
public sealed class CreateWorkItemHandler
    : IRequestHandler<CreateWorkItemCommand, Result<WorkItemDto>>
{ }

// ✅ CORRECT — Sealed record
public sealed record CreateWorkItemCommand(
    Guid ProjectId, string Title, WorkItemType Type
) : IRequest<Result<WorkItemDto>>;

// ❌ INCORRECT — Unsealed without justification
public class WorkItemValidator { }
```

**Allowed unsealed classes:**
- Domain entities with legitimate inheritance hierarchy
- Abstract base classes for shared behavior (e.g., `AuditableEntity`)
- Classes explicitly designed for framework extensibility
- Test helper classes requiring mocking/subclassing

### Modern C# Features (Prefer)

```csharp
// Pattern matching & switch expressions
var result = status switch
{
    WorkItemStatus.Done => "completed",
    WorkItemStatus.Rejected => "rejected",
    _ => "in progress"
};

// Primary constructors
public sealed class WorkItemService(IWorkItemRepository repository, ILogger<WorkItemService> logger)
{ }

// Collection expressions
Permission[] permissions = [Permission.WorkItem_Create, Permission.WorkItem_Update];

// Null checks
ArgumentNullException.ThrowIfNull(command);
if (item is not null) { ... }
```

### Nullable Reference Types

- Design for non-null defaults; validate external inputs at boundaries
- Use `is null` / `is not null` pattern checks
- Prefer `ArgumentNullException.ThrowIfNull(...)` over manual null checks

### Async/Await Best Practices

- Use async/await for all I/O operations
- Surface `CancellationToken` on all public async methods
- Avoid synchronous over async (no `.Result` or `.Wait()`)
- Use `ConfigureAwait(false)` in library code (not needed in ASP.NET Core)

---

## Test-First Development (Non-Negotiable)

### Workflow

1. **Write failing tests** — define expected behavior before implementation
2. **Implement minimal code** — make the tests pass
3. **Refactor** — improve while keeping tests green

### Test Standards

- **Framework:** xUnit + FluentAssertions + NSubstitute
- **Integration tests:** Testcontainers with real PostgreSQL
- **Coverage target:** ≥70% on Application layer

### Theory Pattern Testing (Non-Negotiable)

Use `[Theory]` with `[InlineData]` to eliminate test duplication:

```csharp
// ✅ CORRECT
[Theory]
[InlineData("")]
[InlineData(null)]
public async Task Handle_InvalidTitle_ReturnsValidationError(string? title) { ... }

// ❌ WRONG — Separate [Fact] per value
```

### Test Conventions

- Do NOT emit "Arrange", "Act", "Assert" comments
- Do NOT use magic numbers — use named constants or inline data
- Use test data builders, never raw object construction in tests

```csharp
public sealed class CreateWorkItemTests : IntegrationTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesItem()
    { /* happy path */ }

    [Fact]
    public async Task Handle_MissingTitle_ReturnsValidationError()
    { /* validation */ }

    [Fact]
    public async Task Handle_ViewerRole_ReturnsForbidden()
    { /* permission boundary */ }
}
```

### Test Data Builders

```csharp
var project = ProjectBuilder.New()
    .WithOrganization(orgId)
    .WithStatus(ProjectStatus.Active)
    .Build();
```

---

## CQRS Handler Pattern

```csharp
// Command
public sealed record CreateWorkItemCommand(
    Guid ProjectId,
    Guid? ParentId,
    WorkItemType Type,
    string Title,
    string? Description,
    Priority Priority
) : IRequest<Result<WorkItemDto>>;

// Handler
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
        if (!await permissions.HasPermission(currentUser.Id, request.ProjectId, Permission.WorkItem_Create))
            return Result.Failure<WorkItemDto>(new ForbiddenError());

        // 2. Validate business rules
        // 3. Create entity
        // 4. Persist
        // 5. Publish domain event
        // 6. Return Result.Success(dto)
    }
}
```

## Controller Pattern

```csharp
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Write)]
public sealed class WorkItemsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

---

## Validation & Error Handling

### FluentValidation

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

### Result Pattern — Return, Don't Throw

```csharp
// Return Result<T> instead of throwing exceptions for business errors
var item = await repository.GetByIdAsync(id, ct);
if (item is null)
    return Result.Failure<WorkItemDto>(new NotFoundError("Work item not found"));

return Result.Success(mapper.Map<WorkItemDto>(item));
```

### Result Mapping in Base Controller

```csharp
protected IActionResult HandleResult<T>(Result<T> result)
{
    if (result.IsSuccess) return Ok(result.Value);
    return result.Error switch
    {
        NotFoundError e   => NotFound(Problem(e.Message, statusCode: 404)),
        ForbiddenError    => Forbid(),
        ValidationError e => BadRequest(Problem(e.Message, statusCode: 400)),
        ConflictError e   => Conflict(Problem(e.Message, statusCode: 409)),
        _                 => StatusCode(500, Problem("Internal error", statusCode: 500))
    };
}
```

---

## Message Contract Immutability (Non-Negotiable)

Once domain event contracts are published to RabbitMQ/MassTransit, their namespaces and class names are immutable:

**Allowed operations:**
- ✅ Add new optional properties with default values
- ✅ Add new message types (new classes)
- ✅ Refactor internal logic of consumers

**Prohibited operations:**
- ❌ Change namespace of any message class
- ❌ Rename message classes
- ❌ Remove or rename properties
- ❌ Change property types (breaking schema)
- ❌ Make optional properties required

**Breaking changes protocol:**
1. Create new message version: `WorkItemCreatedDomainEventV2`
2. Keep old message class unchanged
3. Publishers emit both versions during migration
4. Migrate all consumers to handle new version
5. Deprecate old version with minimum 3-month sunset period

---

## Logging Standards

Structured logging with Serilog:

```csharp
// Use ILogger<T> injection
private readonly ILogger<OrderService> _logger;

// Include structured data — use message templates, not string interpolation
_logger.LogInformation("Processing work item {WorkItemId} for project {ProjectId}", workItemId, projectId);

// Log errors with exception object
_logger.LogError(ex, "Failed to create work item for project {ProjectId}", projectId);

// NEVER log sensitive data (passwords, tokens, PII)
```

- Include correlation IDs in all log entries
- Use `ILogger<T>` injection (not static logger)

---

## Database Conventions

- All PKs: `UUID` with `gen_random_uuid()`
- All timestamps: `TIMESTAMPTZ` in UTC
- Soft delete: `deleted_at TIMESTAMPTZ NULL` — never hard delete WorkItems in application code
- All queries filter `WHERE deleted_at IS NULL` by default via EF Core query filter
- `work_item_histories`: NO EF Core delete behavior — append only

### EF Core Best Practices

```csharp
// ✅ Projections + AsNoTracking for read queries
var items = await dbContext.WorkItems
    .AsNoTracking()
    .Where(w => w.ProjectId == projectId)
    .Select(w => new WorkItemDto { Id = w.Id, Title = w.Title })
    .ToListAsync(ct);

// ❌ Loading entire entities for read-only operations
var items = await dbContext.WorkItems
    .Where(w => w.ProjectId == projectId)
    .ToListAsync(ct);  // Tracks all entities unnecessarily
```

---

## API Conventions

- Base route: `/api/v{version}/[controller]`
- Default version: `1.0`
- Error format: `ProblemDetails` — never plain strings
- Pagination: `?page=1&pageSize=20` → `{ items: [], totalCount: N, page: 1, pageSize: 20 }`
- Dates: ISO 8601 UTC — `2026-03-15T09:23:11Z`
- IDs: UUID v4 as strings in JSON
- Swagger/OpenAPI annotations: `[ProducesResponseType]` on all actions

---

## Permission System

```csharp
// Resolution order: Individual → Team → Organization
// NEVER bypass — implement in PermissionChecker only

var can = await permissions.HasPermission(
    userId:     currentUser.Id,
    projectId:  request.ProjectId,
    permission: Permission.WorkItem_Create
);
if (!can) return Result.Failure<WorkItemDto>(new ForbiddenError());
```

**DO NOT write permission logic in controllers, services, or repositories.**
All permission checks go through `IPermissionChecker`.

---

## Rate Limiting Policy Names

```csharp
public static class RateLimitPolicies
{
    public const string Auth       = "auth";        // Login, Register
    public const string Write      = "write";       // POST, PUT, DELETE
    public const string Search     = "search";      // Search endpoints
    public const string BulkAction = "bulk_action"; // Bulk operations
    public const string General    = "general";     // Default GET
}
```

Apply with `[EnableRateLimiting(RateLimitPolicies.Write)]` on controller or action.

---

## Anti-Guessing Rules (Non-Negotiable)

**NEVER guess requirements. ALWAYS ask for clarification if ambiguous.**

- If a user story is vague, ask before implementing
- Use "NEEDS CLARIFICATION" marker in plans for ambiguous items
- Guessing wastes effort — explicit clarification ensures alignment

---

## What Claude Code Should NOT Do

- Write permission resolution logic — human writes this
- Write JWT generation/validation — human reviews all auth code
- Write data archival job — irreversible operations need human review
- Write migration scripts that DROP columns — always flag for human review
- Bypass `IPermissionChecker` — never inline permission checks
- Write directly to `work_item_histories` — always use `IHistoryService`
- Generate seed data with real passwords — use constants like `"Test@1234"`

---

## Dependency Injection

```csharp
// Use primary constructors for services
public sealed class WorkItemService(
    IWorkItemRepository repository,
    ILogger<WorkItemService> logger,
    IOptions<TeamFlowSettings> settings)
{ }

// Registration patterns
services.AddScoped<IWorkItemRepository, WorkItemRepository>();
services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

// Options pattern for configuration
services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
```

---

## Performance Guidelines

- **EF Core:** Use projections + `AsNoTracking()` for read queries
- **Pagination:** All list endpoints must be paginated
- **Backlog with 1000 items:** Must load + filter in <500ms
- **No endpoint:** Should exceed 1 second under normal load
- **Caching:** Use `IMemoryCache` for frequently accessed, rarely changing data

---

## Cross-Phase Rules

1. No feature creep during a phase — new requests go to backlog
2. Bugs from previous phases take priority over new feature work
3. Each phase ends with a demo — all ACs confirmed before moving on
4. All migrations backward compatible — never DROP column in same deploy
5. API contracts don't change without version bump and team agreement
6. No secrets in source control — ever
7. All realtime features have REST fallback — SignalR is UX enhancement, not correctness dependency
8. WorkItemHistories is append-only — no UPDATE or DELETE
