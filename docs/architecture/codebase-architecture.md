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
  └── RateLimiting       — Policy definitions (Auth, Write, Search, BulkAction, General)

TeamFlow.Application
  ├── Features/{Domain}/{Feature}/
  │     ├── {Feature}Command.cs   or  {Feature}Query.cs
  │     ├── {Feature}Handler.cs
  │     └── {Feature}Validator.cs
  └── Common/
        ├── Behaviors/    — ActiveUserBehavior, ValidationBehavior, LoggingBehavior
        ├── Interfaces/   — IWorkItemRepository, IPermissionChecker, IAuthService, etc.
        └── Errors/       — NotFoundError, ForbiddenError, ValidationError, ConflictError

TeamFlow.Domain
  ├── Entities/          — WorkItem, Project, Release, Sprint, User, Team, RefreshToken, etc.
  ├── Enums/             — WorkItemType, WorkItemStatus, Priority, LinkType, SprintStatus, etc.
  └── Events/            — WorkItemDomainEvents, ReleaseDomainEvents, SprintDomainEvents, RetroDomainEvents

TeamFlow.Infrastructure
  ├── Persistence/       — TeamFlowDbContext, EF Core configurations
  ├── Repositories/      — WorkItemRepository, ProjectRepository, ReleaseRepository, WorkItemLinkRepository,
  │                        TeamMemberRepository, ActivityLogRepository
  └── Services/          — AuthService, HistoryService, PermissionChecker

TeamFlow.BackgroundServices
  ├── Consumers/         — DomainEventStoreConsumer, SignalRBroadcastConsumer,
  │                        SprintStartedConsumer, SprintCompletedConsumer
  └── Scheduled/Jobs/    — BurndownSnapshotJob, ReleaseOverdueDetectorJob,
                           StaleItemDetectorJob, EventPartitionCreatorJob
```

### Layer Responsibilities

**Domain** — Entities, value objects, enums, and domain event contracts. No framework dependencies. No outbound dependencies.

**Application** — Business logic only. Defines interfaces that Infrastructure implements. Contains all MediatR handlers and FluentValidation validators. Never references EF Core or external services directly.

**Infrastructure** — EF Core, PostgreSQL (Npgsql), repository implementations, `AuthService`, `HistoryService`, `PermissionChecker`. No business logic. Registers itself via `DependencyInjection.AddInfrastructure()`.

**Api** — ASP.NET Core controllers, middleware, SignalR hub, and rate limiting. Controllers are thin: validate nothing, map nothing, just call `Sender.Send()` and return `HandleResult()`.

**BackgroundServices** — MassTransit consumers and Quartz.NET jobs. Runs as a separate hosted process. Shares `TeamFlowDbContext` and `IBroadcastService` with Api.

---

## HTTP Request Flow

```
HTTP Request
  → CorrelationIdMiddleware (attaches X-Correlation-ID)
  → JWT Authentication middleware (validates Bearer token)
  → [Controller action]
      → Sender.Send(command)
          → ActiveUserBehavior (checks user.IsActive — short-circuit 403 if deactivated)
          → ValidationBehavior (FluentValidation — short-circuit on failure)
          → LoggingBehavior (structured log: start + duration)
          → Handler
              → IPermissionChecker.HasPermissionAsync()  [if mutating]
              → IWorkItemRepository / IProjectRepository / ISprintRepository / etc.
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

## Authentication Flow

```
POST /api/v1/auth/register or /login
  → RegisterHandler / LoginHandler
      → IAuthService.HashPassword() / VerifyPassword()   (BCrypt, work factor 12)
      → IAuthService.GenerateJwt()                       (HMAC-SHA256, 30-min expiry)
      → IAuthService.GenerateRefreshToken()              (64 random bytes, base64)
      → IAuthService.HashToken()                         (SHA-256 before DB storage)
      → persist RefreshToken entity
      → return AuthResponse { accessToken, refreshToken, expiresIn }

POST /api/v1/auth/refresh
  → RefreshTokenHandler
      → hash incoming token → lookup by hash in RefreshTokens
      → validate: not revoked, not expired, user active
      → revoke old token, issue new token pair
      → return AuthResponse

POST /api/v1/auth/logout
  → LogoutHandler
      → revoke current user's refresh token
```

JWT claims: `sub` (userId), `email`, `name`, `jti`. Token expiry: 30 minutes. Refresh token expiry: configured via `Jwt:RefreshTokenExpiryDays`.

---

## Domain Event Flow

```
Handler
  → IPublisher.Publish(SprintStartedDomainEvent)
      → MediatR → MassTransit outbox
          → RabbitMQ exchange: teamflow.events
              → Queue: domain.event.store
                  → DomainEventStoreConsumer
                      → INSERT INTO domain_events (append-only, monthly partitions)
              → Queue: signalr.broadcast
                  → SignalRBroadcastConsumer
                      → IBroadcastService.BroadcastToProjectAsync()
                          → TeamFlowHub → SignalR group: project:{projectId}
              → Queue: sprint.started (sprint-specific events)
                  → SprintStartedConsumer
                      → capacity snapshot, velocity history update
```

### Event Contracts

Domain event classes live in `TeamFlow.Domain.Events`. Once published to RabbitMQ, their namespaces and class names are immutable. New versions require a `V2` suffix, with both versions emitted during migration.

Published events by phase — see `docs/architecture/events.md` for full catalog.

---

## Key Interfaces

### ITeamMemberRepository

Defined in `Application/Common/Interfaces/ITeamMemberRepository.cs`. Implemented by `Infrastructure/Repositories/TeamMemberRepository.cs`. Provides team membership lookups used by the User Profile feature to resolve which teams (and their parent organisations) a user belongs to.

### IActivityLogRepository

Defined in `Application/Common/Interfaces/IActivityLogRepository.cs`. Implemented by `Infrastructure/Repositories/ActivityLogRepository.cs`. Returns paginated `ActivityLogItemDto` records drawn from `work_item_histories` filtered by the current user as actor.

---

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

### ISprintRepository

Implemented by `SprintRepository`. Manages sprint CRUD, sprint-item associations, capacity entries, and burndown data point retrieval.

### IPermissionChecker

Implemented by `PermissionChecker` in Infrastructure. Resolution order: Individual `project_memberships.custom_permissions` → Team role → Organization role. Returns `false` if the user has no membership. Fully wired to real data as of Phase 2.

### IAuthService

Implemented by `AuthService` in Infrastructure. Provides: `GenerateJwt`, `GenerateRefreshToken`, `HashToken`, `HashPassword`, `VerifyPassword`. Human reviews all changes to this service — see `CLAUDE.md`.

### IHistoryService

Wraps `WorkItemHistory` entity creation. Every mutation that changes a field calls `RecordAsync()` with `OldValue` and `NewValue`. The underlying table is append-only — `HistoryService` never issues UPDATE or DELETE.

### IBroadcastService

Abstracts SignalR group sends. Groups:
- `project:{projectId}` — all project-level events
- `sprint:{sprintId}` — sprint board events (burndown updates, item changes)
- `user:{userId}` — personal notifications
- `retro:{sessionId}` — retro session events (Phase 4)

### ICurrentUser

Resolved from `HttpContext` JWT claims. Provides `Id` (userId) and `Email`. Used in every command handler for permission checks.

---

## MediatR Pipeline Behaviors

Registered in Application `DependencyInjection`. Applied to every `IRequest<>` in order:

```
ActiveUserBehavior<TRequest, TResponse>
  → Skips if ICurrentUser.IsAuthenticated == false (anonymous endpoints: login, register)
  → Loads user from IUserRepository
  → If user.IsActive == false: short-circuit with ForbiddenError ("Account deactivated")
  → If active: call next()

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

## Rate Limiting Policies

| Policy constant | Applied to |
|---|---|
| `RateLimitPolicies.Auth` | POST /auth/register, /login, /refresh |
| `RateLimitPolicies.Write` | All POST/PUT/DELETE endpoints |
| `RateLimitPolicies.Search` | Search endpoints |
| `RateLimitPolicies.BulkAction` | Bulk operations |
| `RateLimitPolicies.General` | Default GET endpoints |

---

## SignalR Hub

`TeamFlowHub` (`Api/Hubs/TeamFlowHub.cs`) extends `Hub`. Clients join groups on connection. The hub is a broadcast target — it does not handle client-to-server messages.

`IBroadcastService` is implemented in `Api/Services/` using `IHubContext<TeamFlowHub>` so that `BackgroundServices` can broadcast without a direct Hub reference.

---

## Scheduled Jobs (Quartz.NET)

`BaseJob` (`BackgroundServices/Scheduled/Jobs/BaseJob.cs`) handles the lifecycle for all jobs:

1. Writes a `JobExecutionMetric` row with `Status = "Running"` before execution
2. Calls `ExecuteInternal()` on the subclass
3. Updates the metric row with `Status`, `CompletedAt`, `DurationMs`, and `RecordsProcessed`
4. On exception: sets `Status = "Failed"`, logs the error, rethrows as `JobExecutionException(refireImmediately: false)`

| Job | Schedule | Purpose |
|---|---|---|
| `BurndownSnapshotJob` | Daily | Snapshot remaining/completed points for all active sprints; detects At Risk sprints |
| `ReleaseOverdueDetectorJob` | Daily | Flags releases past their date with incomplete items |
| `StaleItemDetectorJob` | Daily | Flags work items with no updates beyond threshold |
| `EventPartitionCreatorJob` | Monthly | Pre-creates next month's partition on `domain_events` table |

---

## Frontend Structure (Next.js 16)

```
teamflow-web/
├── app/
│   ├── login/          — Login page
│   ├── register/       — Registration page
│   ├── profile/        — User profile (Details, Security, Notifications, Activity tabs)
│   ├── admin/          — System admin area (users, organisations, change-password)
│   ├── onboarding/     — Post-login org selection / first-run flow
│   ├── invite/[token]/ — Invitation acceptance
│   ├── org/[slug]/     — Org-scoped shell with nested project routes
│   ├── projects/
│   │   ├── page.tsx    — Projects list
│   │   └── [projectId]/
│   │       ├── backlog/    — Backlog view
│   │       ├── board/      — Kanban board
│   │       ├── sprints/    — Sprint planning
│   │       ├── releases/   — Releases list
│   │       └── work-items/ — Work item detail
│   └── teams/
│       ├── page.tsx        — Teams list
│       └── [teamId]/       — Team detail
├── components/         — Shared UI components (shadcn/ui base)
│   ├── admin/          — Admin-specific components (UserStatusToggle, ConfirmDialog)
│   ├── profile/        — Profile tab components (ProfileDetails, ProfileSecurity,
│   │                      ProfileNotifications, ProfileActivity)
│   └── auth/           — AuthGuard
└── lib/
    ├── api/            — Axios clients per domain (auth, sprints, teams, work-items, etc.)
    ├── stores/         — Zustand stores (auth-store, backlog-filter, kanban-filter, sidebar, theme)
    ├── hooks/          — TanStack Query hooks
    ├── signalr/        — SignalR connection and group management
    └── contexts/       — React context providers
```

State: TanStack Query for server state, Zustand for client state (auth, filters, UI).
Realtime: `@microsoft/signalr` v8 connects to `TeamFlowHub`; components subscribe to project/sprint groups.

---

## Database Conventions

- All PKs: `UUID` via `gen_random_uuid()`
- All timestamps: `TIMESTAMPTZ` in UTC
- Soft delete: `deleted_at TIMESTAMPTZ NULL` — EF Core global query filter excludes soft-deleted rows
- `work_item_histories` and `domain_events`: never modified after insert
- `domain_events` is range-partitioned by `occurred_at` (monthly partitions); `EventPartitionCreatorJob` pre-creates partitions
- Migrations assembly: `TeamFlow.Infrastructure`

---

## Cross-References

- Full schema: `docs/architecture/data-model.md`
- Domain event catalog and RabbitMQ topology: `docs/architecture/events.md`
- Background job schedule and design: `docs/architecture/background-jobs.md`
- API endpoint reference: `docs/architecture/api-contracts.md`
- Coding standards: `docs/code-standards.md`
