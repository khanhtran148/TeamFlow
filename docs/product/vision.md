# 01 — Product Vision & Technology Decisions

## Product Vision

TeamFlow is an internal project management platform for engineering teams of 9–15 people. Inspired by Azure DevOps and Jira, but designed to be leaner, faster, and tailored to the team's actual workflows.

It replaces spreadsheets and TFS boards with a single source of truth — from backlog grooming and sprint planning, through release management and retrospectives, to analytics and automation.

### Core Principles

- **Developer-first** — fast UI, keyboard shortcuts, drag-drop, minimal friction
- **Realtime by default** — all state changes reflected instantly via SignalR + RabbitMQ
- **Permission-aware** — every action enforced at 3 levels: Organization, Team, Individual
- **Traceability** — item history, audit trail, item linking, release tracking — nothing is lost
- **AI-ready from day one** — data model designed to support AI features without migration
- **Production-ready from day one** — structured logging, health checks, rate limiting, backward-compatible migrations
- **AI features intentionally deferred** — clean foundation first

---

## Technology Decisions

### Full Stack

| Layer | Technology | Rationale |
|---|---|---|
| Frontend | Next.js (App Router) | SSR, file-based routing, TanStack Query, Zustand |
| API | .NET 8 — Controller-based | Clean Architecture + Vertical Slice + MediatR + FluentValidation |
| Database | PostgreSQL | tsvector FTS, JSONB custom fields, pgvector future-ready |
| Message Broker | RabbitMQ + MassTransit | Reliable async, dead-letter queue, transport abstraction |
| Realtime | SignalR (ASP.NET Core) | WebSocket hub — live board, history, retro, release |
| Background Jobs | .NET Hosted Services + Quartz.NET | Email, scheduler, cleanup, event consumer |
| Auth | JWT + Refresh Token | Stateless, per-project role claims |
| Rate Limiting | .NET 7+ built-in | No extra package, per-user + per-IP |
| ORM | EF Core + Npgsql | Code-first migrations, LINQ, PostgreSQL extensions |
| Testing | xUnit + Testcontainers | Real PostgreSQL in Docker for integration tests |
| Local Dev | Docker Compose | One-command full stack |

---

### Realtime Architecture

All state-changing actions follow this non-blocking event-driven flow:

```
API handles action
  → publishes event to RabbitMQ
    → Background Service consumes event
      → broadcasts to SignalR Hub
        → connected clients update instantly
```

**Why RabbitMQ as intermediary (not direct SignalR call):**
- API non-blocking — no waiting for broadcast
- Events survive SignalR restarts — still queued in RabbitMQ
- Background Service can throttle/batch before broadcasting
- Adding new consumers (email, audit) requires zero API changes

---

### Rate Limiting Strategy

| Policy | Endpoint Type | Limit | Algorithm | Key |
|---|---|---|---|---|
| `auth` | Login / Register | 10 req / 15 min | Fixed Window | Per IP |
| `write` | POST / PUT / DELETE | 60 req / min | Sliding Window | Per user |
| `search` | Full-text search | 30 req / min | Fixed Window | Per user |
| `bulk_action` | Bulk operations | 10 req / min | Token Bucket | Per user |
| `global` | All others | 200 req / min | Fixed Window | Per user / IP |

---

### Project Structure

```
TeamFlow.sln
├── src/
│   ├── TeamFlow.Domain/            # Entities, Value Objects, Enums, Domain Events
│   ├── TeamFlow.Application/       # Vertical slices, MediatR, CQRS, Validators
│   │   └── Features/
│   │       ├── WorkItems/
│   │       │   ├── CreateWorkItem/
│   │       │   │   ├── CreateWorkItemCommand.cs
│   │       │   │   ├── CreateWorkItemHandler.cs
│   │       │   │   └── CreateWorkItemValidator.cs
│   │       │   ├── UpdateWorkItem/
│   │       │   └── ...
│   │       ├── Sprints/
│   │       ├── Releases/
│   │       ├── Teams/
│   │       └── Permissions/
│   ├── TeamFlow.Infrastructure/    # EF Core, PostgreSQL, RabbitMQ, JWT
│   ├── TeamFlow.Api/               # Controllers, Middleware, SignalR Hub
│   └── TeamFlow.BackgroundServices/ # Hosted services, Quartz jobs, consumers
└── tests/
    └── TeamFlow.Tests/             # xUnit, Testcontainers, integration tests
```

**Vertical Slice rule:** Each feature folder contains handler + command/query + validator + response DTO. No cross-slice imports. No horizontal layer sprawl.

---

### API Conventions

- Versioning: `/api/v{version}/[controller]`
- Error format: `ProblemDetails` (RFC 7807)
- Response: consistent envelope `{ data, meta, errors }`
- Pagination: `{ items, totalCount, page, pageSize }`
- Dates: ISO 8601 UTC (`2026-03-15T09:23:11Z`)
- IDs: UUID v4

---

### Claude Code Setup Notes

> See [CLAUDE.md](./CLAUDE.md) for full instructions.

Key conventions Claude Code must follow:
- Every handler returns `Result<T>` from CSharpFunctionalExtensions
- All commands/queries go through MediatR — no direct service calls from controllers
- Controllers only call `Sender.Send()` and map result to HTTP response
- No business logic in controllers or infrastructure layer
- All database access through EF Core — no raw SQL except performance-critical queries
