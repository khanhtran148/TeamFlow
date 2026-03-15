---
type: codebase-architecture
description: Architecture overview — layer responsibilities, data flow, event flow, and key interfaces
---

# Codebase Architecture

## Layer Overview

```
TeamFlow.Api
  ├── Controllers        — HTTP entry points; call Sender.Send() only
  ├── Middleware         — CorrelationIdMiddleware
  ├── Hubs               — TeamFlowHub (SignalR)
  └── RateLimiting       — Policy definitions

TeamFlow.Application
  ├── Features/{Domain}/{Feature}/
  │     ├── {Feature}Command.cs   or  {Feature}Query.cs
  │     ├── {Feature}Handler.cs
  │     └── {Feature}Validator.cs
  └── Common/
        ├── Behaviors/    — ValidationBehavior, LoggingBehavior
        ├── Interfaces/   — IWorkItemRepository, IPermissionChecker, etc.
        └── Errors/       — NotFoundError, ForbiddenError, ValidationError, ConflictError

TeamFlow.Domain
  ├── Entities/          — WorkItem, Project, Release, Sprint, User, etc.
  ├── Enums/             — WorkItemType, WorkItemStatus, Priority, LinkType, etc.
  └── Events/            — WorkItemDomainEvents, ReleaseDomainEvents, SprintDomainEvents, RetroDomainEvents

TeamFlow.Infrastructure
  ├── Persistence/       — TeamFlowDbContext, EF Core configurations
  ├── Repositories/      — WorkItemRepository, ProjectRepository, ReleaseRepository, WorkItemLinkRepository
  └── Services/          — HistoryService

TeamFlow.BackgroundServices
  ├── Consumers/         — DomainEventStoreConsumer, SignalRBroadcastConsumer
  └── Scheduled/Jobs/    — BaseJob (Quartz.NET base class)
```

### Layer Responsibilities

**Domain** — Entities, value objects, enums, and domain event contracts. No framework dependencies. No outbound dependencies.

**Application** — Business logic only. Defines interfaces that Infrastructure implements. Contains all MediatR handlers and FluentValidation validators. Never references EF Core or external services directly.

**Infrastructure** — EF Core, PostgreSQL (Npgsql), repository implementations, `HistoryService`. No business logic. Registers itself via `DependencyInjection.AddInfrastructure()`.

**Api** — ASP.NET Core controllers, middleware, SignalR hub, and rate limiting. Controllers are thin: validate nothing, map nothing, just call `Sender.Send()` and return `HandleResult()`.

**BackgroundServices** — MassTransit consumers and Quartz.NET jobs. Runs as a separate hosted process. Shares `TeamFlowDbContext` and `IBroadcastService` with Api.

---

## HTTP Request Flow

```
HTTP Request
  → CorrelationIdMiddleware (attaches X-Correlation-ID)
  → [Controller action]
      → Sender.Send(command)
          → ValidationBehavior (FluentValidation — short-circuit on failure)
          → LoggingBehavior (structured log: start + duration)
          → Handler
              → IPermissionChecker.HasPermissionAsync()  [if mutating]
              → IWorkItemRepository / IProjectRepository / IReleaseRepository
                  → TeamFlowDbContext (EF Core + Npgsql → PostgreSQL)
              → IHistoryService.RecordAsync()            [if mutating]
              → IPublisher.Publish(domainEvent)          [if mutating]
              → return Result<T>
      → HandleResult(result) → HTTP response (ProblemDetails on error)
```

All errors surface as `Result<T>` values, never as thrown exceptions from business logic. `ApiControllerBase.HandleResult<T>()` maps error string prefixes to HTTP status codes:

| Error prefix | HTTP status |
|---|---|
| `NotFound*` | 404 |
| `Forbidden*` | 403 |
| `Conflict*` | 409 |
| *(anything else)* | 400 |

---

## Domain Event Flow

```
Handler
  → IPublisher.Publish(WorkItemCreatedDomainEvent)
      → MediatR → MassTransit outbox
          → RabbitMQ exchange: teamflow.events
              → Queue: domain.event.store
                  → DomainEventStoreConsumer
                      → INSERT INTO domain_events (append-only)
              → Queue: signalr.broadcast
                  → SignalRBroadcastConsumer
                      → IBroadcastService.BroadcastToProjectAsync()
                          → TeamFlowHub → SignalR group: project:{projectId}
                          → Connected frontend clients update in real-time
```

### Event Contracts

Domain event classes live in `TeamFlow.Domain.Events`. Once published to RabbitMQ, their namespaces and class names are immutable. New versions require a `V2` suffix, with both versions emitted during migration.

Currently published events (Phase 1):

| Event | Trigger |
|---|---|
| `WorkItemCreatedDomainEvent` | Work item created |
| `WorkItemStatusChangedDomainEvent` | Status transition |
| `WorkItemEstimationChangedDomainEvent` | Estimation value updated |
| `WorkItemAssignedDomainEvent` | Assignee set |
| `WorkItemUnassignedDomainEvent` | Assignee removed |
| `WorkItemPriorityChangedDomainEvent` | Priority changed |
| `WorkItemLinkAddedDomainEvent` | Link created |
| `WorkItemLinkRemovedDomainEvent` | Link removed |
| `WorkItemRejectedDomainEvent` | Status set to Rejected |
| `WorkItemNeedsClarificationFlaggedDomainEvent` | Status set to NeedsClarification |

---

## Key Interfaces

### IWorkItemRepository

Defined in `Application/Common/Interfaces/IWorkItemRepository.cs`. Implemented by `Infrastructure/Repositories/WorkItemRepository.cs`.

Key methods:

- `GetByIdAsync` / `GetByIdWithDetailsAsync` — includes Assignee and Parent navigation
- `GetBacklogPagedAsync` — full filter matrix + full-text search + pagination
- `GetKanbanItemsAsync` — project-scoped items for board view
- `SoftDeleteCascadeAsync` — soft-deletes item and all descendants, returns affected IDs
- `UpdateSortOrderAsync` — single-item sort order update for reorder

### IProjectRepository

Implemented by `ProjectRepository`. `ListAsync` supports filtering by org, status, and full-text search.

### IReleaseRepository

Implemented by `ReleaseRepository`. `GetItemStatusCountsAsync` returns a `Dictionary<WorkItemStatus, int>` for the release detail panel.

### IWorkItemLinkRepository

Implemented by `WorkItemLinkRepository`. `GetReachableTargetsAsync` performs graph traversal for circular dependency detection before adding a `Blocks` link.

### IPermissionChecker

Resolution order: Individual `project_memberships.custom_permissions` → Team role → Organization role. Returns `false` if the user has no membership. Not yet wired to real data (Phase 1 uses a stub; Phase 2 implements fully).

### IHistoryService

Wraps `WorkItemHistory` entity creation. Every mutation that changes a field calls `RecordAsync()` with `OldValue` and `NewValue`. The underlying table is append-only — `HistoryService` never issues UPDATE or DELETE.

### IBroadcastService

Abstracts SignalR group sends. Groups:
- `project:{projectId}` — all project-level events
- `sprint:{sprintId}` — sprint board events
- `user:{userId}` — personal notifications
- `retro:{sessionId}` — retro session events

### ICurrentUser

Resolved from `HttpContext` in Phase 2. In Phase 1, returns a stub seed user identity.

---

## MediatR Pipeline Behaviors

Registered in Application `DependencyInjection`. Applied to every `IRequest<>` in order:

```
ValidationBehavior<TRequest, TResponse>
  → Runs all IValidator<TRequest> instances
  → If any rule fails: return Result.Failure(validationErrorMessage)
  → If all pass: call next()

LoggingBehavior<TRequest, TResponse>
  → Log: "{RequestType} started"
  → call next()
  → Log: "{RequestType} completed in {ElapsedMs}ms"
```

---

## SignalR Hub

`TeamFlowHub` (`Api/Hubs/TeamFlowHub.cs`) extends `Hub`. Clients join groups on connection. The hub itself does not handle client-to-server messages in Phase 1 — it is a broadcast target only.

`IBroadcastService` is implemented in `Api/Services/` using `IHubContext<TeamFlowHub>` so that `BackgroundServices` can broadcast without a direct Hub reference.

---

## Scheduled Jobs (Quartz.NET)

`BaseJob` (`BackgroundServices/Scheduled/Jobs/BaseJob.cs`) handles the lifecycle for all jobs:

1. Writes a `JobExecutionMetric` row with `Status = "Running"` before execution
2. Calls `ExecuteInternal()` on the subclass
3. Updates the metric row with `Status`, `CompletedAt`, `DurationMs`, and `RecordsProcessed`
4. On exception: sets `Status = "Failed"`, logs the error, rethrows as `JobExecutionException(refireImmediately: false)`

Scheduled jobs defined (Phase 3): `BurndownSnapshotJob`, `ReleaseOverdueDetectorJob`, `StaleItemDetectorJob`, `EventPartitionCreatorJob`.

---

## Database Conventions

- All PKs: `UUID` via `gen_random_uuid()`
- All timestamps: `TIMESTAMPTZ` in UTC
- Soft delete: `deleted_at TIMESTAMPTZ NULL` — EF Core global query filter excludes soft-deleted rows
- `work_item_histories` and `domain_events`: never modified after insert
- `domain_events` is range-partitioned by `occurred_at` (monthly partitions)
- Migrations assembly: `TeamFlow.Infrastructure`

---

## Cross-References

- Full schema: `docs/architecture/data-model.md`
- Domain event catalog and RabbitMQ topology: `docs/architecture/events.md`
- Background job schedule and design: `docs/architecture/background-jobs.md`
- API endpoint reference: `docs/architecture/api-contracts.md`
- Coding standards: `docs/code-standards.md`
