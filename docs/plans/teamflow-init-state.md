# TeamFlow — Init State

## Topic
TeamFlow — Internal project management platform for engineering teams (9-15 people). Azure DevOps/Jira inspired, leaner, AI-ready.

## Discovery Context
- **project_type:** greenfield
- **starting_phase:** Phase 0 — Foundation & Design Ready
- **approach:** Test-First Development (TFD/TDD)
- **framework:** .NET 10 (upgraded from planned .NET 8)
- **git:** Initialize new repository

## Confirmed Tech Stack
| Layer | Technology |
|---|---|
| Frontend | Next.js (App Router), TanStack Query, Zustand |
| API | .NET 10, Controller-based, Clean Architecture + Vertical Slice, MediatR, FluentValidation |
| Database | PostgreSQL, EF Core, Npgsql |
| Broker | RabbitMQ, MassTransit |
| Realtime | SignalR (ASP.NET Core) |
| Background | .NET Hosted Services, Quartz.NET |
| Auth | JWT + Refresh Token |
| Rate Limiting | .NET built-in |
| Testing | xUnit, Testcontainers |
| Local Dev | Docker Compose |

## Solution Structure (Confirmed)
```
TeamFlow.sln
├── core/                              # /Core solution folder
│   ├── TeamFlow.Domain/               # Entities, Value Objects, Enums, Domain Events
│   ├── TeamFlow.Application/          # Vertical slices, MediatR, CQRS, Validators
│   └── TeamFlow.Infrastructure/       # EF Core, PostgreSQL, RabbitMQ, JWT
├── apps/                              # /Apps solution folder
│   ├── TeamFlow.Api/                  # Controllers, Middleware, SignalR Hub
│   ├── TeamFlow.BackgroundServices/   # Hosted services, Quartz jobs, consumers
│   └── teamflow-web/                  # Next.js frontend app
├── tests/
│   └── TeamFlow.Tests/                # xUnit, Testcontainers, integration tests
├── docker-compose.yml
└── planning/                          # Existing planning docs (read-only reference)
```

## Phase 0 Scope — Backend Track
1. Solution structure with solution folders (/Core, /Apps, /tests)
2. All domain entities, value objects, enums (from planning/03-data-model.md)
3. EF Core DbContext, entity configurations, migrations
4. Seed data: 6 user accounts (one per role), sample project, epics, stories, tasks
5. Docker Compose: PostgreSQL, RabbitMQ + management UI
6. API skeleton: versioning, ProblemDetails, pagination, health checks
7. SignalR Hub skeleton
8. MassTransit configuration skeleton
9. Quartz.NET setup skeleton
10. Swagger/OpenAPI auto-generated
11. Test infrastructure: xUnit, Testcontainers, test data builders
12. `docker compose up` starts full backend stack

## Phase 0 Scope — Frontend Track (deferred to second pass)
- Next.js App Router setup
- Design system, layout shell
- TanStack Query, Zustand, SignalR client

## Architecture Rules (from planning/CLAUDE.md)
- Controllers only call `Sender.Send()` — no direct service injection
- All handlers return `Result<T>` from CSharpFunctionalExtensions
- No business logic in Infrastructure or Controllers
- Each feature in its own slice folder — no cross-slice imports
- Permission check in every command handler
- WorkItemHistories append-only — no UPDATE/DELETE
- All API errors return ProblemDetails (RFC 7807)
- Test-First Development: write tests before implementation

## Key Planning References
- planning/01-vision.md — Vision, tech decisions, project structure
- planning/02-roles-permissions.md — 6 roles, 3-level permissions, status flow
- planning/03-data-model.md — Full database schema (ALL tables with SQL)
- planning/04-features.md — Feature inventory, linking, releases
- planning/05-phases.md — Phase breakdown with acceptance criteria
- planning/06-events.md — Domain events, RabbitMQ setup, SignalR hub
- planning/07-background-jobs.md — Background job design, job metrics table
- planning/08-definition-of-done.md — DoD, cross-phase rules, risk register
- planning/CLAUDE.md — Architecture rules, code patterns

## Research Instructions
- Use .NET 10 latest features and APIs
- Use CSharpFunctionalExtensions for Result<T> pattern
- Use MediatR for CQRS
- Use FluentValidation for command validation
- Use MassTransit for RabbitMQ abstraction
- Use Quartz.NET for scheduled jobs
- Follow TFD: write failing tests first, then implement to make them pass
