# TeamFlow

Internal project management platform for engineering teams of 9–15 people. Built on .NET 10 Clean Architecture + Vertical Slice, with realtime updates via SignalR and RabbitMQ.

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | .NET 10, ASP.NET Core (Controller-based), MediatR, FluentValidation |
| Frontend | Next.js (App Router), TanStack Query, Zustand |
| Database | PostgreSQL 17, EF Core + Npgsql |
| Message Broker | RabbitMQ 4 + MassTransit |
| Realtime | SignalR (ASP.NET Core) |
| Background Jobs | .NET Hosted Services + Quartz.NET |
| Auth | JWT + Refresh Token |
| ORM | EF Core (code-first migrations) |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers |

---

## Solution Structure

```
TeamFlow.slnx
├── src/
│   ├── core/
│   │   ├── TeamFlow.Domain/               # Entities, Value Objects, Enums, Domain Events
│   │   ├── TeamFlow.Application/          # Vertical slices, MediatR, CQRS, Validators
│   │   └── TeamFlow.Infrastructure/       # EF Core, PostgreSQL, RabbitMQ, JWT
│   └── apps/
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

## Getting Started

### Prerequisites

- Docker + Docker Compose
- .NET 10 SDK
- Node.js 20+ (for frontend)

### 1. Start infrastructure

```bash
docker compose up postgres rabbitmq -d
```

PostgreSQL is available at `localhost:5432`. RabbitMQ management UI is at `http://localhost:15672` (user: `teamflow`, pass: `teamflow_dev`).

### 2. Run database migrations

```bash
dotnet ef database update \
  --project src/core/TeamFlow.Infrastructure \
  --startup-project src/apps/TeamFlow.Api
```

### 3. Run the API

```bash
dotnet run --project src/apps/TeamFlow.Api
```

API available at `http://localhost:5000`. Swagger UI at `http://localhost:5000/swagger`.

### 4. Run the background services (optional for local dev)

```bash
dotnet run --project src/apps/TeamFlow.BackgroundServices
```

### Full stack via Docker Compose

```bash
docker compose up
```

All services start in dependency order (postgres and rabbitmq health-checked before API and background services start).

---

## API Endpoints

Base path: `/api/v1/`

All responses use `ProblemDetails` (RFC 7807) for errors. All list endpoints are paginated with `?page=1&pageSize=20`.

### Projects — `/api/v1/projects`

| Method | Path | Description |
|---|---|---|
| POST | `/projects` | Create a project |
| GET | `/projects/{id}` | Get project by ID |
| GET | `/projects` | List projects (`?orgId`, `?status`, `?search`, `?page`, `?pageSize`) |
| PUT | `/projects/{id}` | Update project name/description |
| POST | `/projects/{id}/archive` | Archive a project |
| DELETE | `/projects/{id}` | Soft-delete a project |

### Work Items — `/api/v1/workitems`

| Method | Path | Description |
|---|---|---|
| POST | `/workitems` | Create a work item (Epic, Story, Task, Bug, Spike) |
| GET | `/workitems/{id}` | Get work item by ID |
| PUT | `/workitems/{id}` | Update title, description, priority, estimation, acceptance criteria |
| POST | `/workitems/{id}/status` | Transition status |
| DELETE | `/workitems/{id}` | Soft-delete (cascades to children) |
| POST | `/workitems/{id}/move` | Change parent (reparent) |
| POST | `/workitems/{id}/assign` | Assign to a user |
| POST | `/workitems/{id}/unassign` | Remove assignee |
| POST | `/workitems/{id}/links` | Add a link to another work item |
| DELETE | `/workitems/{id}/links/{linkId}` | Remove a link |
| GET | `/workitems/{id}/links` | Get all links for a work item |
| GET | `/workitems/{id}/blockers` | Check if work item is blocked |

### Releases — `/api/v1/releases`

| Method | Path | Description |
|---|---|---|
| POST | `/releases` | Create a release |
| GET | `/releases/{id}` | Get release by ID |
| GET | `/releases` | List releases for a project (`?projectId`, `?page`, `?pageSize`) |
| PUT | `/releases/{id}` | Update release name, description, release date |
| DELETE | `/releases/{id}` | Delete a release |
| POST | `/releases/{id}/items/{workItemId}` | Assign work item to release |
| DELETE | `/releases/{id}/items/{workItemId}` | Remove work item from release |

### Backlog — `/api/v1/backlog`

| Method | Path | Description |
|---|---|---|
| GET | `/backlog` | Get hierarchy-grouped backlog (`?projectId`, `?status`, `?priority`, `?assigneeId`, `?type`, `?sprintId`, `?releaseId`, `?unscheduled`, `?search`, `?page`, `?pageSize`) |
| POST | `/backlog/reorder` | Reorder backlog items |

### Kanban — `/api/v1/kanban`

| Method | Path | Description |
|---|---|---|
| GET | `/kanban` | Get status-grouped board (`?projectId`, `?assigneeId`, `?type`, `?priority`, `?sprintId`, `?releaseId`, `?swimlane`) |

---

## Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/TeamFlow.Domain.Tests
dotnet test tests/TeamFlow.Application.Tests
dotnet test tests/TeamFlow.Infrastructure.Tests
dotnet test tests/TeamFlow.Api.Tests
```

Integration tests use Testcontainers and spin up a real PostgreSQL instance automatically. Docker must be running.

Current test count: **124** (17 domain, 99 application, 8 integration).

---

## Current Status

**Phase 1 — Work Item Management: complete**

- Project CRUD with archive and soft-delete
- Work item hierarchy: Epic > Story > Task / Bug / Spike
- Status transitions with validation, assignment with history tracking
- Item linking with 6 link types and circular blocking detection
- Release management with one-release-per-item constraint
- Backlog query (hierarchy-grouped, filterable, searchable, reorderable)
- Kanban board query (status-grouped, swimlane by assignee or epic)
- Realtime broadcast infrastructure: MassTransit publishes domain events, SignalR hub broadcasts to clients
- Domain events persisted to event store

**Phase 2 — Sprints and Teams** is next. See [docs/process/phases.md](docs/process/phases.md) for the full roadmap.

---

## Documentation

| Document | Description |
|---|---|
| [docs/doc-index.md](docs/doc-index.md) | Master index of all documentation |
| [docs/product/vision.md](docs/product/vision.md) | Product vision, tech decisions, architecture |
| [docs/product/features.md](docs/product/features.md) | Feature inventory |
| [docs/product/roles-permissions.md](docs/product/roles-permissions.md) | 6 roles, permission matrix |
| [docs/architecture/data-model.md](docs/architecture/data-model.md) | Database schema, 21 entities |
| [docs/architecture/events.md](docs/architecture/events.md) | Domain event catalog, SignalR + RabbitMQ architecture |
| [docs/architecture/background-jobs.md](docs/architecture/background-jobs.md) | Background job design |
| [docs/process/phases.md](docs/process/phases.md) | 5-phase delivery plan |
| [docs/process/definition-of-done.md](docs/process/definition-of-done.md) | Definition of Done, cross-phase rules |
| [docs/changelog.md](docs/changelog.md) | What has been built |
| [CLAUDE.md](CLAUDE.md) | Architecture rules and code conventions for Claude Code |
