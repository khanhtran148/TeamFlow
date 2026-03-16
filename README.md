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

### 1. Configure environment

```bash
cp .env.example .env
# Edit .env with your local values (JWT secret, DB password, RabbitMQ credentials)
```

### 2. Start infrastructure

```bash
docker compose up postgres rabbitmq -d
```

PostgreSQL is available at `localhost:5432`. RabbitMQ management UI is at `http://localhost:15672` (credentials from `.env`).

### 3. Run database migrations

```bash
dotnet ef database update \
  --project src/core/TeamFlow.Infrastructure \
  --startup-project src/apps/TeamFlow.Api
```

### 4. Run the API

```bash
dotnet run --project src/apps/TeamFlow.Api
```

API available at `http://localhost:5000`. Swagger UI at `http://localhost:5000/swagger`. Health check at `http://localhost:5000/health`.

### 5. Run the frontend

```bash
cd src/apps/teamflow-web
npm install
npm run dev
```

Frontend available at `http://localhost:3000`.

### 6. Run the background services (optional for local dev)

```bash
dotnet run --project src/apps/TeamFlow.BackgroundServices
```

Background services include Quartz.NET scheduled jobs (BurndownSnapshotJob, ReleaseOverdueDetectorJob, StaleItemDetectorJob, EventPartitionCreatorJob) and MassTransit consumers.

### Full stack via Docker Compose

```bash
docker compose up
```

All services start in dependency order (postgres and rabbitmq health-checked before API and background services start).

### One-command local start (recommended)

Start the entire stack — infrastructure, migrations, API, background services, and frontend — with a single command:

**macOS / Linux:**

```bash
./scripts/start-all.sh
```

**Windows (PowerShell):**

```powershell
.\scripts\start-all.ps1
```

The script waits for each service to be healthy before starting the next. Press Ctrl+C or run `./scripts/start-all.sh stop` to shut everything down cleanly.

| URL | Service |
|---|---|
| `http://localhost:3000` | Frontend |
| `http://localhost:5210` | API + Swagger |
| `http://localhost:15672` | RabbitMQ Management UI |
| `http://localhost:8025` | MailHog (email preview) |

Default admin login: `admin@teamflow.dev` / `Admin@1234`

### Production Docker Compose

```bash
docker compose -f docker-compose.prod.yml up
```

Production profile includes resource limits, health checks on all containers, and `restart: unless-stopped`.

---

## API Endpoints

Base path: `/api/v1/`

All responses use `ProblemDetails` (RFC 7807) for errors. All list endpoints are paginated with `?page=1&pageSize=20`. All endpoints require a valid JWT (Bearer token) unless noted as anonymous.

### Auth — `/api/v1/auth`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | Anonymous | Register with email + password |
| POST | `/auth/login` | Anonymous | Login; returns JWT + refresh token |
| POST | `/auth/refresh` | Anonymous | Exchange refresh token for new JWT |
| POST | `/auth/change-password` | Bearer | Change current password |
| POST | `/auth/logout` | Bearer | Revoke all refresh tokens for current user |

### Projects — `/api/v1/projects`

| Method | Path | Description |
|---|---|---|
| POST | `/projects` | Create a project |
| GET | `/projects/{id}` | Get project by ID |
| GET | `/projects` | List projects (`?orgId`, `?status`, `?search`, `?page`, `?pageSize`) |
| PUT | `/projects/{id}` | Update project name/description |
| POST | `/projects/{id}/archive` | Archive a project |
| DELETE | `/projects/{id}` | Soft-delete a project |
| GET | `/projects/{id}/memberships` | List project memberships |
| POST | `/projects/{id}/memberships` | Add user or team to project with a role |
| DELETE | `/projects/{id}/memberships/{membershipId}` | Remove a project membership |

### Teams — `/api/v1/teams`

| Method | Path | Description |
|---|---|---|
| POST | `/teams` | Create a team |
| GET | `/teams/{id}` | Get team by ID |
| GET | `/teams` | List teams (`?orgId`) |
| PUT | `/teams/{id}` | Update team name/description |
| DELETE | `/teams/{id}` | Delete a team |
| POST | `/teams/{id}/members` | Add a member to the team |
| DELETE | `/teams/{id}/members/{userId}` | Remove a member |
| PUT | `/teams/{id}/members/{userId}/role` | Change a member's role |

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
| GET | `/workitems/{id}/history` | Get paginated history feed (newest first) |

### Sprints — `/api/v1/sprints`

| Method | Path | Description |
|---|---|---|
| POST | `/sprints` | Create a sprint |
| GET | `/sprints` | List sprints for a project (`?projectId`) |
| GET | `/sprints/{id}` | Get sprint detail with items and capacity |
| PUT | `/sprints/{id}` | Update sprint name/goal/dates |
| DELETE | `/sprints/{id}` | Delete a planning-status sprint |
| POST | `/sprints/{id}/start` | Start a sprint (scope locks) |
| POST | `/sprints/{id}/complete` | Complete a sprint (carries over unfinished items) |
| POST | `/sprints/{id}/items/{workItemId}` | Add item to sprint |
| DELETE | `/sprints/{id}/items/{workItemId}` | Remove item from sprint |
| PUT | `/sprints/{id}/capacity` | Update per-member capacity |
| GET | `/sprints/{id}/burndown` | Get burndown chart data (ideal + actual lines) |

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

### Health — `/health`

| Method | Path | Description |
|---|---|---|
| GET | `/health` | Full health check (PostgreSQL + RabbitMQ) |
| GET | `/health/ready` | Readiness probe (tagged checks only) |

---

## Running Tests

```bash
# All backend tests
dotnet test

# Specific test project
dotnet test tests/TeamFlow.Domain.Tests
dotnet test tests/TeamFlow.Application.Tests
dotnet test tests/TeamFlow.Infrastructure.Tests
dotnet test tests/TeamFlow.Api.Tests
dotnet test tests/TeamFlow.BackgroundServices.Tests

# Frontend E2E tests (requires API + frontend running)
cd src/apps/teamflow-web
npx playwright test

# E2E with UI mode (for debugging)
npx playwright test --ui

# E2E for specific feature
npx playwright test e2e/sprints/
```

Integration tests (TeamFlow.Api.Tests) use Testcontainers and start a real PostgreSQL container automatically. Docker must be running.

Current test count: **795 backend** + **77 Playwright E2E** (63 sprint/phase E2E + 14 profile E2E).

---

## Current Status

**Active branch:** `feat/org-management-admin-bootstrap`

**All 5 delivery phases complete.** Recent feature work continues on top of the Phase 5 baseline.

**Completed phases:**

Phase 0 — Foundation: .NET 10 + Next.js solution, full database schema, Docker Compose, test infrastructure.

Phase 1 — Work Item Management: Project CRUD, work item hierarchy (Epic > Story > Task/Bug/Spike), status transitions, item linking with 6 link types and circular blocking detection, release management, backlog query, Kanban board, realtime via SignalR + RabbitMQ.

Phase 2 — Authentication & Authorization: JWT + refresh token auth, bcrypt password hashing, 3-level permission resolution (Individual/Team/Org), team management with Team Manager scope enforcement, work item history UI, Playwright test infrastructure.

Phase 3 — Hardening + Sprint Planning: Sprint planning (11 endpoints + frontend with drag-and-drop and burndown chart), 4 scheduled background jobs, health checks, global exception handler, performance indexes, 513 backend tests + 63 E2E tests.

Phase 4 — Collaboration + Planning: Comment system, Planning Poker, Backlog Refinement, Retrospective, Release Detail page.

Phase 5 — Insights + Automation: Advanced search (tsvector + GIN), Dashboard and Analytics (velocity, burndown, cumulative flow, cycle time, heatmap), Notifications and Reminders (email outbox, in-app, per-user preferences), Background Automation (6 scheduled jobs). 795 backend tests passing.

**Recent additions (post-Phase 5):**
- User Profile Management: `/profile` page with 4 tabs, `AvatarUrl` on User, activity log from work item histories, UserMenu profile link
- Assignee Tooltip: `AssignedAt` on WorkItem, enhanced avatar tooltip showing full name + assignment date across all views
- Developer scripts: `scripts/start-all.sh` (macOS) and `scripts/start-all.ps1` (Windows) for one-command full-stack startup
- Testing rules updated: E2E tests mandatory for every feature; Testcontainers required for all integration tests

See [docs/process/phases.md](docs/process/phases.md) for the full roadmap and [docs/changelog.md](docs/changelog.md) for detailed change history.

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
