---
type: codebase-summary
description: High-level overview of the TeamFlow codebase — structure, patterns, and implementation status
---

# Codebase Summary

## Solution Structure

```
TeamFlow.slnx
├── src/
│   ├── core/                              # Core solution folder
│   │   ├── TeamFlow.Domain/               # Entities, value objects, enums, domain events
│   │   ├── TeamFlow.Application/          # Vertical slices, MediatR handlers, validators
│   │   └── TeamFlow.Infrastructure/       # EF Core, PostgreSQL, repositories, services
│   └── apps/                              # Apps solution folder
│       ├── TeamFlow.Api/                  # Controllers, middleware, SignalR hub
│       ├── TeamFlow.BackgroundServices/   # MassTransit consumers, Quartz.NET jobs
│       └── teamflow-web/                  # Next.js 16 frontend (App Router)
└── tests/
    ├── TeamFlow.Tests.Common/             # Shared builders, IntegrationTestBase
    ├── TeamFlow.Domain.Tests/             # Domain entity and enum unit tests
    ├── TeamFlow.Application.Tests/        # Handler, validator, behavior tests
    ├── TeamFlow.Infrastructure.Tests/     # EF Core and repository integration tests
    ├── TeamFlow.Api.Tests/                # Controller and API integration tests
    └── TeamFlow.BackgroundServices.Tests/ # Consumer and Quartz job tests
```

## Project Dependency Graph

```
TeamFlow.Api
  └── TeamFlow.Application
        └── TeamFlow.Domain

TeamFlow.Infrastructure
  └── TeamFlow.Application (interfaces only)

TeamFlow.BackgroundServices
  ├── TeamFlow.Infrastructure
  └── TeamFlow.Application

TeamFlow.*.Tests
  ├── TeamFlow.Tests.Common
  └── [project under test]
```

The Domain layer has no outbound dependencies. The Application layer depends only on Domain. Infrastructure and Api depend on Application through interfaces — no layer references a higher layer.

## Key Patterns

### Vertical Slice Architecture

Each feature lives in its own folder under `Application/Features/{Domain}/{FeatureName}/`. A slice contains a command or query record, a handler, and optionally a validator. Slices do not import from each other at the handler level.

```
Application/Features/Sprints/CreateSprint/
  CreateSprintCommand.cs   — IRequest<Result<SprintDto>>
  CreateSprintHandler.cs   — IRequestHandler
  CreateSprintValidator.cs — AbstractValidator
```

### CQRS via MediatR

Commands mutate state and return `Result` or `Result<T>`. Queries read state and return `Result<T>`. Controllers call only `Sender.Send()` — no direct service injection.

### Result\<T\> Pattern

All handlers return `CSharpFunctionalExtensions.Result<T>`. Errors are returned as values, not thrown as exceptions. `ApiControllerBase.HandleResult<T>()` maps error prefixes (`NotFound`, `Forbidden`, `Conflict`) to the correct HTTP status codes with `ProblemDetails` bodies.

### MediatR Pipeline Behaviors

Three registered pipeline behaviors apply to every request in order:

1. `ActiveUserBehavior` — checks `user.IsActive`; short-circuits with 403 if user is deactivated (skips for anonymous endpoints)
2. `ValidationBehavior` — runs FluentValidation; short-circuits with a 400 error if invalid
3. `LoggingBehavior` — logs request type and duration using structured logging

### Event-Driven Architecture

Handlers publish domain events via `IPublisher.Publish()`. MassTransit routes events over RabbitMQ to consumers in `TeamFlow.BackgroundServices`:

- `DomainEventStoreConsumer` — persists every event to the `domain_events` partitioned table
- `SignalRBroadcastConsumer` — broadcasts to SignalR groups via `IBroadcastService`
- `SprintStartedConsumer` — handles sprint start side effects (capacity snapshot, velocity history)
- `SprintCompletedConsumer` — handles sprint completion side effects (velocity recording, snapshot finalization)

### Permission Resolution

All permission checks go through `IPermissionChecker.HasPermissionAsync()`. Resolution order: Individual → Team → Organization. Checks happen inside command handlers, never in controllers or repositories.

---

## What Is Implemented

### Phase 0 — Foundation (complete)

- Full PostgreSQL schema with all tables including AI-ready tables (`domain_events`, `sprint_snapshots`, `burndown_data_points`, `team_velocity_history`, `work_item_embeddings`, `ai_interactions`)
- EF Core migrations and seed data
- MassTransit + RabbitMQ configuration
- SignalR hub skeleton (`TeamFlowHub`)
- Quartz.NET setup with `BaseJob` checkpoint pattern
- API versioning, `ProblemDetails` error format, correlation ID middleware
- Test infrastructure: xUnit, Testcontainers, `IntegrationTestBase`, test data builders
- Docker Compose full-stack configuration

### Phase 1 — Work Item Management (complete)

**Projects** — create, get by ID, list with filter/search/pagination, update, archive, delete

**Work Items** — full CRUD, parent-child hierarchy (Epic → UserStory → Task/Bug/Spike), soft delete with cascade, status transitions, assign/unassign, move to new parent

**Work Item Linking** — add link (6 types: Blocks, RelatesTo, Duplicates, DependsOn, Causes, Clones), remove link, get all links grouped by type, check blockers, circular detection via graph traversal

**Work Item History** — paginated chronological feed via `GET /api/v1/workitems/{id}/history`

**Backlog** — paginated list with filters (status, priority, assignee, type, sprint, release, unscheduled, full-text search), reorder (sort order)

**Kanban** — board view grouped by status columns (ToDo, InProgress, InReview, Done), filters, blocked item detection

**Releases** — create, get, list paginated, update, delete, assign/unassign work item

**Organizations** — create, get by ID, list

### Phase 2 — Auth & Authorization (complete)

**Authentication** — register (POST `/api/v1/auth/register`), login (POST `/api/v1/auth/login`), refresh token (POST `/api/v1/auth/refresh`), change password (POST `/api/v1/auth/change-password`), logout (POST `/api/v1/auth/logout`). Login response includes `mustChangePassword` flag.

**JWT** — 30-minute access tokens (HMAC-SHA256), 64-byte cryptographically random refresh tokens stored as SHA-256 hashes, BCrypt password hashing (work factor 12)

**Teams** — create team, get by ID, list (paginated, org-scoped), update, delete, add/remove members, change member role

**Project Memberships** — add member to project (user or team), remove member, list memberships, get current user's permissions (`GET /api/v1/projects/{id}/memberships/me`)

**Permission enforcement** — `IPermissionChecker` fully wired to real `project_memberships` data; 3-level resolution (Individual → Team → Organization) active on all mutating endpoints

**Rate limiting** — `Auth` policy on register/login/refresh; `Write` policy on all mutation endpoints

### Phase 3 — Sprint Planning & Hardening (complete)

**Sprints** — create, get by ID, list (paginated, project-scoped), update, delete, start, complete, add/remove work items, update per-member capacity, get burndown chart data

**Domain Events** — `SprintStartedDomainEvent`, `SprintCompletedDomainEvent`, `SprintItemAddedDomainEvent`, `SprintItemRemovedDomainEvent` all published and consumed

**Background Jobs (Quartz.NET)**:
- `BurndownSnapshotJob` — daily snapshot of remaining/completed points per active sprint; detects "At Risk" when remaining > ideal × 1.2; broadcasts `burndown.updated` via SignalR
- `ReleaseOverdueDetectorJob` — detects releases past their release date with incomplete items; publishes `ReleaseOverdueDetectedDomainEvent`
- `StaleItemDetectorJob` — flags work items with no updates beyond a threshold; publishes `WorkItemStaleFlaggedDomainEvent`
- `EventPartitionCreatorJob` — pre-creates monthly partitions on the `domain_events` table

**MassTransit Consumers**:
- `SprintStartedConsumer` — reacts to sprint start event
- `SprintCompletedConsumer` — reacts to sprint completion event

**Integration tests** — full test suite across all six test projects (130 test files); covers Auth, Teams, Sprints, ProjectMemberships, background jobs, and all Phase 1 features

**Frontend** — Next.js 16 (App Router) with pages for: login, register, projects list, project detail (backlog, board, sprints, releases, work items), teams list, team detail

### Org Management & Admin Improvements (complete)

**Org Management** — Organization creation with slug; member invitations (email invite, accept, reject, revoke); member management (role change, remove); `GET /api/v1/organizations/{slug}` by slug with IsActive guard

**Admin Panel** — SystemAdmin bootstrap via seed (`MustChangePassword = true`); force password change flow on first login; admin-initiated password reset (`POST /admin/users/{userId}/reset-password`, sets `MustChangePassword = true`); paginated + searchable admin grids for users and organizations; user/org deactivation and activation with token revocation and pending invitation cleanup; org name + slug update; org ownership transfer

**Admin Frontend** — `/admin/users` (search, pagination, reset password dialog, status toggle), `/admin/organizations` (search, pagination, edit dialog, transfer ownership dialog, status toggle), `/admin/change-password` (force-change page on first login), `/deactivated` (static error page), logout button in admin layout, 403 deactivation interceptor redirects to `/deactivated`

**Test count:** 1015 (73 domain + 745 application + 25 background services + 141 API + 31 infrastructure)

---

## Domain Entities

| Entity | Phase | Description |
|---|---|---|
| `User` | 0 | Authenticated user with BCrypt password hash; `IsActive` (default true), `MustChangePassword` (default false) |
| `Organization` | 0 | Top-level tenant; `IsActive` (default true) |
| `Project` | 0 | Work container within an org |
| `ProjectMembership` | 0 | Links user or team to a project with a role |
| `Team` | 0 | Group of users within an org |
| `TeamMember` | 0 | User membership in a team with a role |
| `WorkItem` | 0 | Epic / UserStory / Task / Bug / Spike |
| `WorkItemLink` | 0 | Directional link between two work items |
| `WorkItemHistory` | 0 | Append-only audit trail for work item changes |
| `Release` | 0 | Named release within a project |
| `Sprint` | 0 | Time-boxed iteration within a project |
| `RefreshToken` | 2 | Hashed refresh token with expiry |
| `BurndownDataPoint` | 0/3 | Daily burndown snapshot per sprint |
| `SprintSnapshot` | 0/3 | Sprint scope snapshot at start/end |
| `TeamVelocityHistory` | 0/3 | Historical velocity per team/sprint |
| `RetroSession` | 0 | Retrospective session (Phase 4) |
| `RetroCard` | 0 | Card submitted in a retro |
| `RetroVote` | 0 | Vote on a retro card |
| `RetroActionItem` | 0 | Action item from a retro |
| `WorkItemEmbedding` | 0 | AI vector embedding (Phase 4+) |
| `AiInteraction` | 0 | AI suggestion record (Phase 4+) |
| `JobExecutionMetric` | 0 | Quartz job run history |

---

## Domain Events Published

| Event | Phase | Trigger |
|---|---|---|
| `WorkItemCreatedDomainEvent` | 1 | Work item created |
| `WorkItemStatusChangedDomainEvent` | 1 | Status transition |
| `WorkItemEstimationChangedDomainEvent` | 1 | Estimation updated |
| `WorkItemAssignedDomainEvent` | 1 | Assignee set |
| `WorkItemUnassignedDomainEvent` | 1 | Assignee removed |
| `WorkItemPriorityChangedDomainEvent` | 1 | Priority changed |
| `WorkItemLinkAddedDomainEvent` | 1 | Link created |
| `WorkItemLinkRemovedDomainEvent` | 1 | Link removed |
| `WorkItemRejectedDomainEvent` | 1 | Status set to Rejected |
| `WorkItemNeedsClarificationFlaggedDomainEvent` | 1 | Status set to NeedsClarification |
| `WorkItemStaleFlaggedDomainEvent` | 3 | StaleItemDetectorJob fires |
| `ReleaseCreatedDomainEvent` | 1 | Release created |
| `ReleaseItemAssignedDomainEvent` | 1 | Work item assigned to release |
| `ReleaseStatusChangedDomainEvent` | 1 | Release status changes |
| `ReleaseOverdueDetectedDomainEvent` | 3 | ReleaseOverdueDetectorJob fires |
| `SprintStartedDomainEvent` | 3 | Sprint started |
| `SprintCompletedDomainEvent` | 3 | Sprint completed |
| `SprintItemAddedDomainEvent` | 3 | Work item added to sprint |
| `SprintItemRemovedDomainEvent` | 3 | Work item removed from sprint |

---

## What Is Next

**Phase 6 — Retrospectives** (planned)

- Retro sessions: create, start, add cards, reveal, vote, close
- Action items linked to backlog
- Retro domain events published and consumed

**Frontend testing infrastructure** — Vitest + Testing Library not yet configured; recommended before next frontend phase with logic-bearing hooks.

See `docs/process/phases.md` for full scope and acceptance criteria.
