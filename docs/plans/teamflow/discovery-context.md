# TeamFlow — Discovery Context

## Project Description
TeamFlow is an internal project management platform for engineering teams of 9-15 people. Inspired by Azure DevOps and Jira — leaner, faster, AI-ready. Replaces spreadsheets and TFS boards with a single source of truth for backlog grooming, sprint planning, release management, retrospectives, analytics, and automation.

## Tech Stack (Confirmed)
| Layer | Technology |
|---|---|
| Frontend | Next.js (App Router), TanStack Query, Zustand |
| API | .NET 8, Controller-based, Clean Architecture + Vertical Slice, MediatR, FluentValidation |
| Database | PostgreSQL, EF Core, Npgsql |
| Broker | RabbitMQ, MassTransit |
| Realtime | SignalR (ASP.NET Core) |
| Background | .NET Hosted Services, Quartz.NET |
| Auth | JWT + Refresh Token |
| Rate Limiting | .NET 7+ built-in |
| Testing | xUnit, Testcontainers |
| Local Dev | Docker Compose |

## Git Strategy
Initialize new repository with .gitignore, README, and initial commit.

## Starting Phase
Phase 0 — Foundation & Design Ready (Weeks 1-2)

## Architecture
- Clean Architecture + Vertical Slice (each feature: command/query + handler + validator + response DTO)
- All handlers return `Result<T>` from CSharpFunctionalExtensions
- Controllers only call `Sender.Send()` — no business logic
- Permission checks in handlers via `IPermissionChecker`
- WorkItemHistories append-only — no UPDATE/DELETE
- API errors: ProblemDetails (RFC 7807)
- Event-driven: API → RabbitMQ → Background Service → SignalR → clients

## Planning Documents Available
- `planning/01-vision.md` — Product vision, tech decisions, project structure
- `planning/02-roles-permissions.md` — 6 roles, 3-level permission system, status flow
- `planning/03-data-model.md` — Full database schema with AI-ready tables
- `planning/04-features.md` — Feature inventory, item linking, release management
- `planning/05-phases.md` — 5-phase breakdown with acceptance criteria
- `planning/06-events.md` — Domain event catalog, RabbitMQ/SignalR architecture
- `planning/07-background-jobs.md` — Background job design per phase
- `planning/08-definition-of-done.md` — DoD, cross-phase rules, risk register
- `planning/CLAUDE.md` — Architecture rules, code patterns, conventions
