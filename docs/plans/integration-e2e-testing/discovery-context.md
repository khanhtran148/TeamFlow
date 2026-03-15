# Discovery Context -- Integration & E2E Testing Strategy

## Scope
Fullstack (backend integration tests + frontend E2E tests)

## Requirements
Full coverage strategy: integration tests for all backend handlers (Application layer, API controllers, repositories, background jobs) + Playwright E2E tests for critical user flows. Goal is a comprehensive, long-term testing foundation for all future phases.

## Context
- Long-term strategy: building a testing foundation that scales across all future phases
- Not just Phase 3 hardening -- this is an architectural decision for the project's testing approach
- Must work in CI pipeline

## Preferences
- Testcontainers + Playwright: real PostgreSQL for backend integration tests, real browser for E2E
- No mocks for integration tests -- use real DB
- Playwright for all E2E/browser tests

## Current State (from Scout)

### Backend
- 6 backend test projects exist, Application tests use NSubstitute mocks
- `WebApplicationFactory` NuGet installed in `TeamFlow.Api.Tests` but **NOT used** -- all "API" tests actually call `ISender.Send()` directly, bypassing HTTP pipeline
- `public partial class Program` already exists in `Program.cs`
- No repository integration tests beyond `PermissionChecker`
- Each test class starts its own Postgres container via `IntegrationTestBase` (slow at scale)
- Existing test helpers: `TestCurrentUser`, `AlwaysAllowTestPermissionChecker`, `NullBroadcastService`, `TestHistoryService`
- 9 test data builders in `TeamFlow.Tests.Common`

### Frontend
- Playwright set up with 17 E2E spec files across auth, permissions, sprints, work-items, releases, teams, smoke, rate-limit
- Two fixture files: `auth.ts` (test.extend pattern) and `sprint-helpers.ts` (raw functions, no test.extend)
- Auth via `localStorage.setItem` injection -- no `storageState` reuse
- No `data-testid` attributes on any components
- `playwright.config.ts` has single `chromium` project, no setup project for global auth
- 2 page objects exist (`login.page.ts`, `register.page.ts`) but inconsistently used
- Sprint component library: 8 components in `components/sprints/`

### Controllers (Sprint -- 11 endpoints)
1. `POST /api/v1/sprints` -- Create
2. `GET /api/v1/sprints` -- List
3. `GET /api/v1/sprints/{id}` -- GetById
4. `PUT /api/v1/sprints/{id}` -- Update
5. `DELETE /api/v1/sprints/{id}` -- Delete
6. `POST /api/v1/sprints/{id}/start` -- Start
7. `POST /api/v1/sprints/{id}/complete` -- Complete
8. `POST /api/v1/sprints/{id}/items/{workItemId}` -- AddItem
9. `DELETE /api/v1/sprints/{id}/items/{workItemId}` -- RemoveItem
10. `PUT /api/v1/sprints/{id}/capacity` -- UpdateCapacity
11. `GET /api/v1/sprints/{id}/burndown` -- GetBurndown

### Permission Matrix (6 roles x 5 sprint permissions)
Roles: OrgAdmin, ProductOwner, TechnicalLeader, TeamManager, Developer, Viewer
Permissions: Sprint_View, Sprint_Create, Sprint_Start, Sprint_Complete, Sprint_Edit
