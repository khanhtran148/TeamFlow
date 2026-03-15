# Scout Findings: Integration & E2E Testing Landscape
Date: 2026-03-15
Scout area: testing infrastructure across all layers

---

## 1. Backend Test Projects — Structure and Dependencies

### 1.1 Project Inventory

| Project | Framework / Key Packages | Type of Tests |
|---|---|---|
| `TeamFlow.Tests.Common` | Testcontainers.PostgreSql 4.11.0, xUnit 2.9.3 | Shared base — not a test runner |
| `TeamFlow.Domain.Tests` | FluentAssertions 8.8.0, coverlet | Pure unit — no DB, no containers |
| `TeamFlow.Application.Tests` | NSubstitute 5.3.0, FluentAssertions, coverlet | Unit with mocks — no DB |
| `TeamFlow.Infrastructure.Tests` | Testcontainers.PostgreSql, NSubstitute, coverlet | Integration — real Postgres |
| `TeamFlow.Api.Tests` | Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing 10.0.5, NSubstitute | Integration — real Postgres via ISender, no HTTP layer |
| `TeamFlow.BackgroundServices.Tests` | MassTransit.TestFramework 9.0.1, EF Core InMemory + SQLite, NSubstitute | Unit/light-integration — SQLite in-memory |

Key observation: `Microsoft.AspNetCore.Mvc.Testing` is installed in `TeamFlow.Api.Tests` but **no `WebApplicationFactory` is instantiated anywhere** in the codebase. The package is present but entirely unused.

### 1.2 Test Base Class: `IntegrationTestBase`

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Tests.Common/IntegrationTestBase.cs`

Pattern:
- Implements `IAsyncLifetime` — each test class gets its own `PostgreSqlContainer` (postgres:17).
- Container lifecycle: start in `InitializeAsync`, dispose in `DisposeAsync`.
- Seeds one `Organization` (`SeedOrgId`) and one `User` (`SeedUserId`) before each test class.
- Exposes `protected IServiceProvider Services` and `protected TeamFlowDbContext DbContext`.
- Virtual `ConfigureServices(IServiceCollection)` hook — subclasses wire up real repositories and stub interfaces.
- **No shared container / `IClassFixture` pattern**: every test class spins up its own container. This is the slow but isolation-safe approach.

### 1.3 Builders Available

Directory: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Tests.Common/Builders/`

Builders present:
- `OrganizationBuilder`
- `ProjectBuilder`
- `ProjectMembershipBuilder`
- `ReleaseBuilder`
- `SprintBuilder`
- `TeamBuilder`
- `UserBuilder`
- `WorkItemBuilder`
- `WorkItemHistoryBuilder`
- `WorkItemLinkBuilder`
- `BurndownDataPointBuilder`

All builders are consumed by both Application unit tests and Background Services tests.

### 1.4 Test Stubs (defined locally in `TestHelpers.cs`)

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/TestHelpers.cs`

- `TestCurrentUser` — returns fixed `SeedUserId`
- `AlwaysAllowTestPermissionChecker` — always returns `true` / `Developer` role
- `NullBroadcastService` — no-op SignalR broadcast

These stubs are also duplicated (slightly) in `TeamFlow.Application.Tests` — no shared stub library exists for Application-layer tests.

---

## 2. Application Layer Handler Tests

Directory: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Application.Tests/Features/`

### 2.1 Coverage by Feature Area

All handlers are tested with **NSubstitute mocks** — no real database.

| Feature Area | Test Files |
|---|---|
| Auth | `ChangePasswordTests`, `LoginTests`, `LogoutTests`, `RefreshTokenTests`, `RegisterTests` |
| WorkItems | `AddLinkTests`, `AssignWorkItemTests`, `ChangeStatusTests`, `CheckBlockersTests`, `CreateWorkItemTests`, `DeleteWorkItemTests`, `GetLinksTests`, `GetWorkItemHistoryTests`, `GetWorkItemTests`, `MoveWorkItemTests`, `RemoveLinkTests`, `UnassignWorkItemTests`, `UpdateWorkItemTests` |
| Projects | `ArchiveProjectTests`, `CreateProjectTests`, `DeleteProjectTests`, `GetProjectTests`, `ListProjectsTests`, `UpdateProjectTests` |
| Sprints | `AddItemTests`, `CompleteSprintTests`, `CreateSprintTests`, `DeleteSprintTests`, `GetBurndownTests`, `GetSprintTests`, `ListSprintsTests`, `RemoveItemTests`, `StartSprintTests`, `UpdateCapacityTests`, `UpdateSprintTests` |
| Backlog | `GetBacklogTests`, `ReorderBacklogTests` |
| Kanban | `GetKanbanBoardTests` |
| Releases | `AssignItemTests`, `CreateReleaseTests`, `DeleteReleaseTests`, `GetReleaseTests`, `ListReleasesTests`, `UnassignItemTests`, `UpdateReleaseTests` |
| Teams | `AddMemberTests`, `ChangeTeamMemberRoleTests`, `CreateTeamTests`, `DeleteTeamTests`, `GetTeamTests`, `ListTeamsTests`, `RemoveTeamMemberTests`, `UpdateTeamTests` |
| ProjectMemberships | `AddProjectMemberTests`, `GetMyPermissionsTests`, `ListProjectMembershipsTests`, `RemoveProjectMemberTests` |

Cross-cutting:
- `ValidationBehaviorTests` — tests the MediatR validation pipeline behavior
- `PagedResultTests` — tests pagination value object

### 2.2 Handler Test Pattern (CreateWorkItem as canonical example)

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Application.Tests/Features/WorkItems/CreateWorkItemTests.cs`

- Constructor initialises all mocked dependencies via `NSubstitute.Substitute.For<T>()`
- Default mocks wired in constructor: permission = true, AddAsync returns input entity
- Factory method `CreateHandler()` keeps construction separate from test body
- `[Theory]` + `[InlineData]` used for multi-value cases (e.g., `WorkItemType.Task/Bug/Spike`)
- Validators tested independently via `ValidateAsync` — not through MediatR pipeline
- **No `[Collection]` or `IClassFixture`** — each test class is fully isolated with fresh mocks

### 2.3 Gaps in Application Tests

- No `[Theory]` usage found for permission boundary tests — permission denied cases are each a separate `[Fact]`
- `Events/` subfolder does not appear in `Features/` — no domain event handler tests found (e.g., no `WorkItemCreatedDomainEventHandler` tests)
- No test for the `LoggingBehavior` or `PerformanceBehavior` pipeline behaviors if they exist

---

## 3. Infrastructure Tests

Directory: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Infrastructure.Tests/Services/`

Files found: `PermissionCheckerTests.cs`

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Infrastructure.Tests/Services/PermissionCheckerTests.cs`

Pattern:
- Extends `IntegrationTestBase` (real Postgres via Testcontainers)
- Manual seed via `SeedTestData()` private method + lazy `_seeded` guard
- Tests the full permission resolution hierarchy: Individual > Team > Organization role
- Covers: OrgAdmin bypass, Developer permissions, Viewer restrictions, team-inherited roles, individual overrides, no-membership returns false
- `ConfigureServices` is empty — relies only on `DbContext` from base class

Gap: Only one service (`PermissionChecker`) is integration-tested at the Infrastructure level. No repository integration tests exist (e.g., no `WorkItemRepositoryTests`, `ProjectRepositoryTests`, etc.).

---

## 4. API Integration Tests

Directory: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/`

### 4.1 Pattern Used

Despite the `Microsoft.AspNetCore.Mvc.Testing` package being present, **no HTTP-level tests exist**. All "API tests" are actually handler-level integration tests that call `ISender.Send()` directly through a `ServiceCollection` wired with real repositories against a Postgres container.

Files and their pattern:
- `WorkItems/WorkItemHierarchyTests.cs` — lifecycle test: create Epic/Story/Task, cascade delete, verify soft-delete in DB
- `WorkItems/ItemLinkingTests.cs` — create link, check blockers, remove link
- `Projects/ProjectLifecycleTests.cs` — create, update, archive, list, delete project via `ISender`
- `Releases/ReleaseAssignmentTests.cs` — create release, assign item, verify one-release constraint, delete unlinks
- `Hubs/TeamFlowHubTests.cs` — unit test of `TeamFlowHub` with NSubstitute mocks; no real SignalR connection

### 4.2 Auth in Tests

No JWT generation or HTTP auth headers exist in any backend test. Auth is bypassed with:
1. `TestCurrentUser` stub (fixed known user ID)
2. `AlwaysAllowTestPermissionChecker` (always returns true)

There are no tests for the negative permission path at the HTTP level — only at the handler level.

### 4.3 Rate Limiting Tests

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/RateLimiting/AuthRateLimitTests.cs`

Pure unit tests — verify `RateLimitSettings` default property values only. No integration test spins up the ASP.NET Core pipeline to verify actual rate limiting middleware behaviour.

### 4.4 Security Tests

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/Security/AuthorizationTests.cs`

Reflection-based tests that verify `[Authorize]` attribute is on `ApiControllerBase` and that all controllers inherit from it. No real HTTP call is made.

### 4.5 Gap Summary for API Tests

- `WebApplicationFactory` is installed but unused — no HTTP-level integration tests
- No test calls a controller action via `HttpClient`
- No test verifies that a 401 is returned for an unauthenticated request
- No test verifies that a 403 is returned for an authorised but unpermitted request at the HTTP layer
- No test verifies `ProblemDetails` response shape
- No test verifies rate limit middleware returns 429 with `Retry-After` header at HTTP level
- No test verifies SignalR hub authentication over a real connection

---

## 5. Background Job Tests

Directory: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/`

### 5.1 DbContext Factory

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/TestDbContextFactory.cs`

- Uses **SQLite in-memory** (not Testcontainers Postgres) — fast but not production-equivalent
- `TestTeamFlowDbContext` subclass overrides `OnModelCreating` to replace PostgreSQL-specific column types (`jsonb`, `timestamptz`, `vector(1536)`) with SQLite-compatible nulls
- FK enforcement disabled via `PRAGMA foreign_keys = OFF` — avoids needing full entity graph per test

### 5.2 Job Tests

Files:
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Jobs/BurndownSnapshotJobTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Jobs/EventPartitionCreatorJobTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Jobs/ReleaseOverdueDetectorJobTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Jobs/StaleItemDetectorJobTests.cs`

Pattern (BurndownSnapshotJob as canonical):
- `IDisposable` — disposes `DbContext` after each test
- Mocked: `IBroadcastService`, `ILogger<T>`, `IJobExecutionContext`
- Real: SQLite `TeamFlowDbContext`, builder-constructed entities seeded directly into `_dbContext`
- Tests call `ExecuteJobAsync(_jobContext, metric)` directly — not via Quartz scheduler
- Verifies: DB side-effects, SignalR broadcast calls, warning log calls

### 5.3 Consumer Tests

Files:
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintCompletedConsumerTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintStartedConsumerTests.cs`

Uses `MassTransit.TestFramework` — real in-memory bus with consumer registered. Harness-based testing pattern.

### 5.4 Domain Tests (inside BackgroundServices.Tests)

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Jobs/SprintTests.cs`

Tests sprint domain entity behaviour directly — sits in the wrong project but works.

### 5.5 Gaps

- No Quartz scheduler integration test — no test verifies that jobs are registered with correct cron schedules
- No test verifies that a consumer correctly handles a poison message or a failed consumer pipeline
- No test verifies MassTransit retry/outbox behavior

---

## 6. Domain Tests

Directory: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Domain.Tests/`

Files:
- `Entities/EntityTests.cs` — entity creation and invariant tests
- `EntityTests.cs` — top-level (likely a catch-all)
- `EnumTests.cs` — verifies enum values are as expected

No Testcontainers, no EF Core — pure logic. No gaps observed for a domain test layer.

---

## 7. Frontend Test Structure

### 7.1 package.json

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/package.json`

Scripts:
```
"e2e": "playwright test"
"e2e:ui": "playwright test --ui"
"e2e:headed": "playwright test --headed"
```

No `test` script exists. No Jest, Vitest, React Testing Library, or `@testing-library/*` packages are present in `dependencies` or `devDependencies`. The only test tooling is `@playwright/test ^1.58.2`.

**There are zero component unit tests or frontend integration tests.**

### 7.2 Playwright Config

File: `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/playwright.config.ts`

Key settings:
- `testDir: "./e2e"`
- `fullyParallel: false` — serial execution (comment: avoids rate limit exhaustion)
- `workers: 1` — single worker (comment: within auth rate limit 10 req/15 min)
- `retries: 2` on CI, `0` locally
- Browser: Chromium only
- `baseURL: http://localhost:3000` (env: `BASE_URL`)
- API target: `http://localhost:5210/api/v1` (env: `API_URL`)
- `webServer`: starts `npm run dev`, reuses existing server locally

Note: The comment mentions "10 req/15 min" rate limit which contradicts the backend `RateLimitSettings` default of 30 req/min. This may reflect an older version of the settings or a different concern.

---

## 8. E2E Test Structure

### 8.1 Directory Layout

```
src/apps/teamflow-web/e2e/
├── smoke.spec.ts
├── z-rate-limit.spec.ts
├── auth/
│   ├── auth-flow.spec.ts
│   └── stale-flag.spec.ts        (misplaced — tests stale item flag, not auth)
├── fixtures/
│   ├── auth.ts                   (Playwright test.extend fixture — register/login helpers)
│   └── sprint-helpers.ts         (API setup helpers: registerUser, createProject, createSprint, etc.)
├── history/
│   └── history-tracking.spec.ts
├── pages/
│   ├── login.page.ts             (Page Object Model)
│   └── register.page.ts          (Page Object Model)
├── permissions/
│   ├── individual-override.spec.ts
│   ├── permission-denial.spec.ts
│   └── role-ui.spec.ts
├── releases/
│   └── overdue-release.spec.ts
├── sprints/
│   ├── burndown-chart.spec.ts
│   ├── sprint-backlog.spec.ts
│   └── sprint-planning.spec.ts
├── teams/
│   └── team-management.spec.ts
└── work-items/
    └── stale-flag.spec.ts
```

### 8.2 Fixture Pattern

Two fixture approaches coexist:

1. `e2e/fixtures/auth.ts` — `test.extend<>()` pattern extending Playwright's base test with `apiUrl`, `testUser`, `registerTestUser`, `loginTestUser` fixtures. Returns typed `TestUser` objects.

2. `e2e/fixtures/sprint-helpers.ts` — standalone async functions (`registerUser`, `createProject`, `createSprint`, `createWorkItem`, `addItemToSprint`, `startSprint`, `completeSprint`, `createRelease`) used directly in `beforeAll` blocks. Returns typed seeded entity objects.

**These two fixture approaches are inconsistent.** The `auth.ts` extended fixture is only used in `auth/auth-flow.spec.ts`. All sprint/permission/stale-flag tests use the standalone helper functions directly.

### 8.3 Authentication Strategy in E2E Tests

Two strategies are used:

1. **UI-based auth** — `page.goto('/register')`, fill form, click submit (used in `auth-flow.spec.ts`)
2. **API-seeded auth + `localStorage` injection** — call `POST /auth/register` via `request`, then call `authenticatePage()` which navigates to `/login` and uses `page.evaluate()` to inject the Zustand auth state directly into `localStorage` with key `teamflow-auth`

The `authenticatePage` helper in `sprint-helpers.ts` is the dominant pattern — it bypasses the UI login entirely. The Zustand store shape expected: `{ state: { accessToken, refreshToken, isAuthenticated: true }, version: 0 }`.

### 8.4 Spec Coverage by Area

| Spec File | What It Tests | Approach |
|---|---|---|
| `smoke.spec.ts` | App loads, title matches | UI |
| `auth/auth-flow.spec.ts` | Register via UI, login via UI, token refresh via API | UI + API |
| `auth/stale-flag.spec.ts` | Misplaced — actually tests stale item detector | API + UI |
| `work-items/stale-flag.spec.ts` | Stale item API + kanban board renders | API + UI |
| `history/history-tracking.spec.ts` | History endpoint exists, append-only (401/404/405 checks) | API only |
| `permissions/permission-denial.spec.ts` | Viewer/Dev API calls return 403/404 | API only |
| `permissions/individual-override.spec.ts` | Permission override behaviour | API/UI |
| `permissions/role-ui.spec.ts` | Role-based UI elements visible/hidden | UI |
| `sprints/sprint-planning.spec.ts` | Create sprint via UI, add items, start/complete sprint | API + UI |
| `sprints/sprint-backlog.spec.ts` | Backlog panel, drag-and-drop | API + UI |
| `sprints/burndown-chart.spec.ts` | Burndown chart renders | API + UI |
| `releases/overdue-release.spec.ts` | Overdue release detection | API + UI |
| `teams/team-management.spec.ts` | Team create, add/remove members | API + UI |
| `z-rate-limit.spec.ts` | Auth endpoint returns 429 + Retry-After header | API only |

### 8.5 Page Objects

Files in `e2e/pages/`:
- `login.page.ts` — wraps login form interactions
- `register.page.ts` — wraps registration form interactions

Page objects exist but are not consistently used — most tests drive the UI directly without them.

---

## 9. Identified Gaps and Issues

### 9.1 No HTTP-Level API Integration Tests

The `Microsoft.AspNetCore.Mvc.Testing` package is present in `TeamFlow.Api.Tests` but zero tests use `WebApplicationFactory` or `HttpClient`. This means:
- No test verifies that the ASP.NET Core middleware pipeline (auth, rate limiting, error handling, routing) works end-to-end
- No test verifies `401 Unauthorized` for missing JWT
- No test verifies `403 Forbidden` for insufficient permissions at the HTTP response level
- No test verifies `ProblemDetails` response shape (RFC 7807 compliance)
- No test verifies response serialization (e.g., camelCase JSON, UUID format)

### 9.2 No Frontend Unit/Component Tests

No Jest, Vitest, or React Testing Library. There are zero component tests, hook tests, or utility function tests for the Next.js frontend. Only E2E tests exist for the frontend.

### 9.3 No Shared `IClassFixture` for Expensive Containers

Each `IntegrationTestBase` subclass spins up its own Postgres container. There is no `[Collection]` sharing a single container across multiple test classes. For a large test suite, this will be slow. However, isolation is maximised.

### 9.4 E2E Test Fixture Inconsistency

Two parallel fixture patterns exist (`test.extend` in `auth.ts` vs standalone functions in `sprint-helpers.ts`). Most tests ignore the `auth.ts` fixture entirely. A unified fixture approach would reduce duplication.

### 9.5 Stale-Flag Test Misplacement

`e2e/auth/stale-flag.spec.ts` is placed in the `auth/` folder but tests the stale item detection feature (a background job concern), not auth flows.

### 9.6 No Quartz Job Schedule Verification

Background job tests call `ExecuteJobAsync` directly. No test verifies that Quartz schedules are registered with the correct cron expressions at application startup.

### 9.7 No MassTransit Retry / Outbox Tests

Consumer tests exist but do not cover poison message handling, retry exhaustion, or the outbox pattern.

### 9.8 Missing Repository Integration Tests

Infrastructure layer has only `PermissionCheckerTests`. No integration tests for repositories (e.g., `WorkItemRepository`, `ProjectRepository`) exist despite real Postgres being available.

### 9.9 E2E Tests Have No `data-testid` Selectors

Most E2E selectors rely on visible text or ARIA roles (e.g., `page.getByText("To Do")`). No `data-testid` attributes are used. Several stale-flag tests contain commented-out `data-testid` checks noting this as a future improvement.

### 9.10 E2E Tests Lack Cleanup / Teardown

Tests create real entities via API in `beforeAll` but there is no `afterAll` cleanup. This means test data accumulates in the running API's database. Long-running test suites will interfere with each other.

### 9.11 No Playwright Global Setup File

No `globalSetup` or `globalTeardown` is configured in `playwright.config.ts`. There is no shared authenticated state file (e.g., `storageState`) pre-generated for reuse across tests.

---

## 10. Key File Paths Reference

### Backend

- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Tests.Common/IntegrationTestBase.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Tests.Common/Builders/` (11 builder files)
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Tests.Common/TeamFlow.Tests.Common.csproj`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Application.Tests/TeamFlow.Application.Tests.csproj`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Application.Tests/ValidationBehaviorTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Application.Tests/Features/WorkItems/CreateWorkItemTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Application.Tests/Features/Auth/RegisterTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/TeamFlow.Api.Tests.csproj`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/TestHelpers.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/WorkItems/WorkItemHierarchyTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/Projects/ProjectLifecycleTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/Releases/ReleaseAssignmentTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/Hubs/TeamFlowHubTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/Security/AuthorizationTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Api.Tests/RateLimiting/AuthRateLimitTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.Infrastructure.Tests/Services/PermissionCheckerTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/TestDbContextFactory.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Jobs/BurndownSnapshotJobTests.cs`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintCompletedConsumerTests.cs`

### Frontend / E2E

- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/package.json`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/playwright.config.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/fixtures/auth.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/fixtures/sprint-helpers.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/smoke.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/auth/auth-flow.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/sprints/sprint-planning.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/work-items/stale-flag.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/permissions/permission-denial.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/history/history-tracking.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/z-rate-limit.spec.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/pages/login.page.ts`
- `/Users/trankhanh/Desktop/MyProjects/TeamFlow/src/apps/teamflow-web/e2e/pages/register.page.ts`
