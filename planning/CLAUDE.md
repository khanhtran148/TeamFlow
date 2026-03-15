# CLAUDE.md — TeamFlow Instructions for Claude Code

> This file provides context and conventions for Claude Code when working on the TeamFlow codebase.
> Update this file whenever a new pattern is established or a convention changes.
> Every PR introducing a new architectural pattern must update this file.

---

## Project Overview

TeamFlow is an internal project management platform.

- **API:** .NET 8, controller-based, Clean Architecture + Vertical Slice
- **Frontend:** Next.js (App Router)
- **Database:** PostgreSQL via EF Core + Npgsql
- **Messaging:** RabbitMQ via MassTransit
- **Realtime:** SignalR
- **Background Jobs:** .NET Hosted Services + Quartz.NET
- **Auth:** JWT + Refresh Token

Full planning: see `README.md` and linked documents.

---

## Architecture Rules — Never Violate

1. **Controllers only call `Sender.Send()`** — no direct service injection in controllers
2. **All handlers return `Result<T>` or `Result`** from CSharpFunctionalExtensions
3. **No business logic in Infrastructure layer** — only persistence and external service calls
4. **No business logic in Controllers** — only HTTP mapping
5. **Each feature in its own slice folder** — no cross-slice imports at the handler level
6. **Permission check in every command handler that modifies data** — not in controllers
7. **WorkItemHistories is append-only** — no UPDATE or DELETE ever written against this table
8. **All API errors return ProblemDetails** (RFC 7807) — never plain strings

---

## Solution Structure

```
src/
├── TeamFlow.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Enums/
│   └── Events/
├── TeamFlow.Application/
│   ├── Features/
│   │   ├── WorkItems/
│   │   │   ├── CreateWorkItem/
│   │   │   │   ├── CreateWorkItemCommand.cs
│   │   │   │   ├── CreateWorkItemHandler.cs
│   │   │   │   ├── CreateWorkItemValidator.cs
│   │   │   │   └── CreateWorkItemResponse.cs
│   │   │   ├── UpdateWorkItemStatus/
│   │   │   ├── AssignWorkItem/
│   │   │   └── GetBacklog/
│   │   ├── Sprints/
│   │   ├── Releases/
│   │   ├── Teams/
│   │   └── Permissions/
│   ├── Common/
│   │   ├── Behaviors/          # MediatR pipeline behaviors
│   │   ├── Errors/             # Domain error types
│   │   └── Interfaces/         # Repository interfaces
│   └── DependencyInjection.cs
├── TeamFlow.Infrastructure/
│   ├── Persistence/
│   │   ├── TeamFlowDbContext.cs
│   │   ├── Configurations/     # EF Core entity configs
│   │   └── Migrations/
│   ├── Repositories/
│   └── DependencyInjection.cs
├── TeamFlow.Api/
│   ├── Controllers/
│   │   └── Base/
│   │       └── ApiControllerBase.cs
│   ├── Middleware/
│   ├── RateLimiting/
│   └── Program.cs
└── TeamFlow.BackgroundServices/
    ├── EventDriven/Consumers/
    ├── Scheduled/
    └── Program.cs
```

---

## Code Patterns

### Handler Pattern

```csharp
// Command
public record CreateWorkItemCommand(
    Guid ProjectId,
    Guid? ParentId,
    WorkItemType Type,
    string Title,
    string? Description,
    Priority Priority
) : IRequest<Result<WorkItemDto>>;

// Handler
public class CreateWorkItemHandler : IRequestHandler<CreateWorkItemCommand, Result<WorkItemDto>>
{
    private readonly IWorkItemRepository _repository;
    private readonly IPermissionChecker _permissions;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;

    public async Task<Result<WorkItemDto>> Handle(
        CreateWorkItemCommand request,
        CancellationToken ct)
    {
        // 1. Permission check — always first
        if (!await _permissions.HasPermission(_currentUser.Id, request.ProjectId, Permission.WorkItem_Create))
            return Result.Failure<WorkItemDto>(new ForbiddenError());

        // 2. Validate business rules
        // 3. Create entity
        // 4. Persist
        // 5. Publish domain event
        // 6. Return Result.Success(dto)
    }
}
```

### Controller Pattern

```csharp
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Write)]
public class WorkItemsController : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkItemCommand cmd,
        CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }
}
```

### Result Mapping in Base Controller

```csharp
protected IActionResult HandleResult<T>(Result<T> result)
{
    if (result.IsSuccess) return Ok(result.Value);
    return result.Error switch
    {
        NotFoundError e    => NotFound(Problem(e.Message, statusCode: 404)),
        ForbiddenError     => Forbid(),
        ValidationError e  => BadRequest(Problem(e.Message, statusCode: 400)),
        ConflictError e    => Conflict(Problem(e.Message, statusCode: 409)),
        _                  => StatusCode(500, Problem("Internal error", statusCode: 500))
    };
}
```

### Publishing Domain Events

```csharp
// After successful persistence, publish event
await _publisher.Publish(new WorkItemCreatedDomainEvent(
    workItem.Id,
    workItem.ProjectId,
    workItem.Type,
    workItem.Title,
    _currentUser.Id
), ct);
```

### History Writing

```csharp
// Always use history service, never write directly
await _historyService.RecordAsync(new WorkItemHistoryEntry(
    WorkItemId: workItem.Id,
    ActorId: _currentUser.Id,
    ActionType: "StatusChanged",
    FieldName: "Status",
    OldValue: oldStatus.ToString(),
    NewValue: newStatus.ToString()
), ct);
```

---

## Domain Enums

```csharp
public enum WorkItemType    { Epic, UserStory, Task, Bug, Spike }
public enum WorkItemStatus  { ToDo, InProgress, InReview, NeedsClarification, Done, Rejected }
public enum Priority        { Critical, High, Medium, Low }
public enum ProjectRole     { OrgAdmin, ProductOwner, TechnicalLeader, TeamManager, Developer, Viewer }
public enum LinkType        { Blocks, RelatesTo, Duplicates, DependsOn, Causes, Clones }
public enum LinkScope       { SameProject, CrossProject }
public enum ReleaseStatus   { Unreleased, Overdue, Released }
```

---

## Permission System

```csharp
// Resolution order: Individual → Team → Organization
// NEVER bypass this — implement in PermissionChecker only

// Usage in any handler:
var can = await _permissions.HasPermission(
    userId:     _currentUser.Id,
    projectId:  request.ProjectId,
    permission: Permission.WorkItem_Create
);
if (!can) return Result.Failure<WorkItemDto>(new ForbiddenError());
```

**DO NOT write permission logic in controllers, services, or repositories.**  
All permission checks go through `IPermissionChecker`.

---

## Database Conventions

- All PKs: `UUID` with `gen_random_uuid()`
- All timestamps: `TIMESTAMPTZ` in UTC
- Soft delete: `deleted_at TIMESTAMPTZ NULL` — never hard delete WorkItems in application code
- All queries filter `WHERE deleted_at IS NULL` by default via EF Core query filter
- `work_item_histories`: NO EF Core delete behavior — no cascade, no soft delete, append only

### EF Core Query Filter (Global)
```csharp
// In DbContext
modelBuilder.Entity<WorkItem>()
    .HasQueryFilter(w => w.DeletedAt == null);
```

---

## API Conventions

- Base route: `/api/v{version}/[controller]`
- Default version: `1.0`
- Error format: `ProblemDetails` — never plain strings
- Pagination: `?page=1&pageSize=20` → `{ items: [], totalCount: N, page: 1, pageSize: 20 }`
- Dates: ISO 8601 UTC — `2026-03-15T09:23:11Z`
- IDs: UUID v4 as strings in JSON

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

## Testing Conventions

### Integration Test Structure
```csharp
public class CreateWorkItemTests : IntegrationTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesItem()
    { /* happy path */ }

    [Fact]
    public async Task Handle_MissingTitle_ReturnsValidationError()
    { /* validation */ }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNotFound()
    { /* not found */ }

    [Fact]
    public async Task Handle_ViewerRole_ReturnsForbidden()
    { /* permission boundary */ }
}
```

### Test Data Builders
```csharp
// Use builders, never raw object construction in tests
var project = ProjectBuilder.New()
    .WithOrganization(orgId)
    .WithStatus(ProjectStatus.Active)
    .Build();
```

---

## MassTransit Consumer Pattern

```csharp
public class WorkItemStatusChangedConsumer
    : IConsumer<WorkItemStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<WorkItemStatusChangedEvent> context)
    {
        // 1. Check idempotency
        // 2. Business logic
        // 3. Persist DomainEvent
        // 4. Broadcast SignalR
        // Exceptions → MassTransit retry policy handles
    }
}
```

---

## SignalR Broadcast Pattern

```csharp
// Always broadcast through the service, not directly from consumers
await _broadcastService.BroadcastToProjectAsync(
    projectId: workItem.ProjectId,
    eventName: "workitem.status_changed",
    payload:   new { workItemId, fromStatus, toStatus }
);
```

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

## Custom Commands (add to `.claude/commands/`)

### `/create-feature {name} {aggregate}`
Scaffold a new vertical slice:
- `{Name}Command.cs` or `{Name}Query.cs`
- `{Name}Handler.cs`
- `{Name}Validator.cs`
- `{Name}Response.cs`
- `{Name}Tests.cs` with happy path + validation + not found + forbidden tests

### `/create-consumer {event-type}`
Scaffold a new MassTransit consumer:
- `{EventType}Consumer.cs` with idempotency check pattern
- Registration in `MassTransitConsumerSetup.cs`

### `/create-job {job-name} {schedule}`
Scaffold a new Quartz.NET job:
- `{JobName}Job.cs` with checkpoint pattern + metrics
- Registration in `JobScheduler.cs`

### `/create-migration {description}`
Generate EF Core migration after entity changes.
Always review before applying — especially for ALTER/DROP operations.
