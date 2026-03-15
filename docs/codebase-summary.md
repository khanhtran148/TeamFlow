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
│       └── teamflow-web/                  # Next.js 15 frontend (App Router)
└── tests/
    ├── TeamFlow.Tests.Common/             # Shared builders, IntegrationTestBase
    ├── TeamFlow.Domain.Tests/             # Domain entity and enum unit tests
    ├── TeamFlow.Application.Tests/        # Handler, validator, behavior tests
    ├── TeamFlow.Infrastructure.Tests/     # EF Core and repository integration tests
    └── TeamFlow.Api.Tests/                # Controller and API integration tests
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
Application/Features/WorkItems/CreateWorkItem/
  CreateWorkItemCommand.cs   — IRequest<Result<WorkItemDto>>
  CreateWorkItemHandler.cs   — IRequestHandler
  CreateWorkItemValidator.cs — AbstractValidator
```

### CQRS via MediatR

Commands mutate state and return `Result` or `Result<T>`. Queries read state and return `Result<T>`. Controllers call only `Sender.Send()` — no direct service injection.

### Result\<T\> Pattern

All handlers return `CSharpFunctionalExtensions.Result<T>`. Errors are returned as values, not thrown as exceptions. `ApiControllerBase.HandleResult<T>()` maps error prefixes (`NotFound`, `Forbidden`, `Conflict`) to the correct HTTP status codes with `ProblemDetails` bodies.

### MediatR Pipeline Behaviors

Two registered pipeline behaviors apply to every request in order:

1. `ValidationBehavior` — runs FluentValidation; short-circuits with a 400 error if invalid
2. `LoggingBehavior` — logs request type and duration using structured logging

### Event-Driven Architecture

Handlers publish domain events via `IPublisher.Publish()`. MassTransit routes events over RabbitMQ to two consumers in `TeamFlow.BackgroundServices`:

- `DomainEventStoreConsumer` — persists every event to the `domain_events` partitioned table
- `SignalRBroadcastConsumer` / `SignalRWorkItemStatusBroadcastConsumer` — broadcasts to SignalR groups via `IBroadcastService`

### Permission Resolution

All permission checks go through `IPermissionChecker.HasPermissionAsync()`. Resolution order: Individual → Team → Organization. Checks happen inside command handlers, never in controllers or repositories.

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

### Phase 1 — Work Item Management (complete, 124 tests passing)

**Projects** — create, get by ID, list with filter/search/pagination, update, archive, delete

**Work Items** — full CRUD, parent-child hierarchy (Epic → UserStory → Task/Bug/Spike), soft delete with cascade, status transitions, assign/unassign, move to new parent

**Work Item Linking** — add link (6 types: blocks, relates_to, duplicates, depends_on, causes, clones), remove link, get all links grouped by type, check blockers, circular detection via graph traversal

**Backlog** — paginated list with filters (status, priority, assignee, type, sprint, release, unscheduled, full-text search), reorder (sort order)

**Kanban** — board view grouped by status columns (ToDo, InProgress, InReview, Done), filters, blocked item detection, swimlane parameter (not yet applied server-side)

**Releases** — create, get, list paginated, update, delete, assign work item to release, unassign work item from release

**Domain Events published** — `WorkItemCreatedDomainEvent`, `WorkItemStatusChangedDomainEvent`, `WorkItemEstimationChangedDomainEvent`, `WorkItemAssignedDomainEvent`, `WorkItemUnassignedDomainEvent`, `WorkItemPriorityChangedDomainEvent`, `WorkItemLinkAddedDomainEvent`, `WorkItemLinkRemovedDomainEvent`, `WorkItemRejectedDomainEvent`, `WorkItemNeedsClarificationFlaggedDomainEvent`

## What Is Next

**Phase 2 — Authentication & Authorization** (Weeks 6–8)

- Register, login, JWT + refresh token, silent refresh, logout, change password
- Team management: create team, add/remove members, assign Team Manager role
- Permission enforcement on all Phase 1 endpoints (3-level resolution)
- Work Item History UI: chronological feed, realtime updates

See `docs/process/phases.md` for full scope and acceptance criteria.
