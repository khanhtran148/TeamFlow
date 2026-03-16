# Changelog

## [Unreleased]

### User Profile Management + Assignee Tooltip (2026-03-16)

#### User Profile — Backend

- `AvatarUrl` (nullable string) added to `User` entity; `AddAvatarUrlToUser` EF Core migration applied
- `GetProfileQuery` handler: returns own profile (id, display name, email, avatar URL, role, org); no permission check — users access only their own data
- `UpdateProfileCommand` handler: updates display name and avatar URL with validation; writes history entry
- `GetActivityLogQuery` handler: paginated (page size capped at 50) activity feed from `work_item_histories` for the current user, newest first
- Integration tests written first (TFD); all 3 handlers covered with happy path, validation, and edge-case scenarios

#### User Profile — Frontend

- `/profile` page with 4 tabs: Details (name, email, avatar URL), Security (change password form), Notifications (per-type preference toggles), Activity (paginated log)
- TanStack Query hooks for `useProfile`, `useUpdateProfile`, `useActivityLog`
- `UserMenu` component updated with "Profile" link navigating to `/profile`
- Activity log renders work item title, action type (color-coded), and relative timestamp with pagination

#### Assignee Tooltip — Backend

- `AssignedAt` (`DateTimeOffset?`) added to `WorkItem` entity; EF Core migration applied
- `AssignWorkItemHandler` sets `AssignedAt = DateTimeOffset.UtcNow` on assignment; `UnassignWorkItemHandler` clears it to null
- `WorkItemDto` and all relevant response DTOs updated to include `assignedAt` field
- Integration tests cover `AssignedAt` being set, cleared, and reflected in DTOs

#### Assignee Tooltip — Frontend

- `assignedAt` field added to TypeScript `WorkItemDto` and assignee-related types
- `UserAvatar` tooltip enhanced: now shows full display name and formatted assignment date (e.g., "Assigned Mar 14, 2026")
- All assignee avatar usages updated: Backlog rows, Kanban cards, Work Item Detail children tab, and Assignee Picker

#### Testing Rules Update (CLAUDE.md)

- Rule 11 added: E2E tests are mandatory for every new feature; existing E2E tests must be updated when behavior changes
- Rule 12 added: Testcontainers required for all unit and integration tests — real PostgreSQL preferred over in-memory mocks

#### E2E Tests — User Profile

- 14 Playwright E2E tests added covering: profile page load, tab navigation, display name update, avatar URL update, change password (success + validation), notification preference toggle, activity log pagination, UserMenu profile link navigation
- Tests run against real API via Testcontainers; storage state reused from global auth setup

#### Developer Scripts

- `scripts/start-all.sh` (macOS/Linux): single command to start full stack — Docker infra (PostgreSQL, RabbitMQ, MailHog), EF Core migrations, .NET API, Background Services, Next.js frontend; waits for health checks before proceeding; `stop` argument or Ctrl+C gracefully terminates all processes
- `scripts/start-all.ps1` (Windows PowerShell): equivalent script for Windows developers; same startup sequence and health-check logic

---

### Phase 3 — Hardening + Sprint Planning (2026-03-15)

#### Sprint Planning Backend (Phase 3.0 + 3.1)

- Sprint domain entity sealed with `Start()`, `Complete()`, `CanAddItem()` domain methods
- 3 new repository interfaces: `ISprintRepository`, `IBurndownDataPointRepository`, `ISprintSnapshotRepository`
- 11 Sprint endpoints: CRUD, start, complete, add/remove item, capacity management, burndown query
- Sprint scope locking: active sprints require TeamManager/OrgAdmin to add items
- Per-member capacity tracking stored as JSONB; capacity utilization computed on read
- Domain events published for sprint start, complete, and item add/remove
- History recorded for all sprint item movements via `IHistoryService`
- Permission enforcement on every mutating handler (Sprint_Create, Sprint_Start, Sprint_Close, Sprint_Edit)
- 42 new Sprint application tests; 0 regressions

#### Sprint Planning Frontend (Phase 3.2)

- Sprint list and detail pages (project-scoped) with dark/light mode support
- Sprint planning board: split-view backlog + sprint scope with `@dnd-kit` drag-and-drop
- Capacity indicator bar: color-coded (green/yellow/red), over-capacity alert with `role="meter"` accessibility
- Per-member capacity breakdown with individual progress bars
- Burndown chart (ideal vs actual lines) built with Recharts; responsive, theme-aware
- Scope lock indicator and add-item confirmation dialog for active sprints
- Permission-aware buttons: start/complete/edit/delete hidden for unauthorized roles
- SignalR event handlers for `Sprint.Started`, `Sprint.Completed`, `Sprint.ItemAdded`, `Sprint.ItemRemoved`, `Burndown.Updated`
- Sprints navigation tab added to project layout

#### Background Jobs (Phase 3.3)

- `BurndownSnapshotJob` (11:59 PM daily): writes daily burndown data for all active sprints; flags "At Risk" when remaining > ideal × 1.2
- `ReleaseOverdueDetectorJob` (00:05 AM daily): transitions overdue releases to Overdue status, publishes `ReleaseOverdueDetectedDomainEvent`
- `StaleItemDetectorJob` (08:00 AM daily): flags items not updated in 14 days via `ai_metadata.stale_flag`; skips done, rejected, and archived-project items
- `EventPartitionCreatorJob` (25th of month, 03:00 AM): creates next month's PostgreSQL partition for `domain_events` table; idempotent
- `SprintStartedConsumer`: creates OnStart snapshot, initializes first burndown data point, broadcasts `sprint.started` via SignalR
- `SprintCompletedConsumer`: creates final snapshot, records team velocity history, broadcasts `sprint.completed` via SignalR
- New `TeamFlow.BackgroundServices.Tests` project with 25 tests; all jobs and consumers tested with NSubstitute mocks and SQLite in-memory context

#### Hardening (Phase 3.4)

- 13 new Application test files covering Teams, Releases, ProjectMemberships, WorkItemHistory (40+ new tests)
- Sprint domain entity status-transition tests (12 tests)
- Theory-pattern validator tests added for `UpdateRelease`, `UpdateSprint`, `UpdateWorkItem` validators
- `idx_wi_project_status_priority` and `idx_wi_sprint_status` partial indexes added via migration `20260315153122_AddPerformanceIndexes.cs`
- `AsNoTracking()` applied to all read-only repository queries
- `LoggingBehavior` and `CorrelationIdMiddleware` sealed and converted to primary constructors
- Health checks: PostgreSQL (Unhealthy on failure) and RabbitMQ (Degraded on failure) at `/health` and `/health/ready`
- `GlobalExceptionHandlerMiddleware`: catches unhandled exceptions, logs with correlation ID, returns RFC 7807 ProblemDetails
- Docker Compose production profile (`docker-compose.prod.yml`) with resource limits, health checks, rolling update documentation
- Hardcoded fallback credentials removed from `BackgroundServices/Program.cs`; fail-fast on missing config
- Total test suite: 373 passing / 381 (8 pre-existing Api.Tests failures unrelated to Phase 3)

#### Integration and E2E Tests (Phase 3.5 + Integration Testing Plan)

- Testcontainers-based `ApiIntegrationTestBase` with Respawn DB reset, real JWT, NullBroadcastService
- Sprint CRUD and lifecycle integration tests (25 tests)
- Permission matrix integration tests: 66 theory-based tests covering all roles against Sprint endpoints
- Rate limiting integration tests (3 tests)
- Health check integration tests (4 tests)
- ProblemDetails shape conformance tests (5 tests)
- Playwright E2E: 21 tests across sprint planning lifecycle, backlog interaction, burndown chart, stale-item flag, overdue release
- Playwright infrastructure: global auth setup with `storageState`, unified fixtures, `data-testid` attributes on all interactive sprint components
- Visual regression baselines: 14 screenshots across light and dark mode
- Cross-page navigation tests (5 tests)
- Permission-based UI tests across Viewer, Developer, TeamManager roles (9 tests)
- 63 total Playwright tests discoverable

#### Test Builder Enhancements (Refactor)

- Bogus 35.6.5 integrated into `TeamFlow.Tests.Common`
- `FakerProvider` sealed static class; thread-safe per-call `Faker` instances
- 7 builders updated to generate realistic fake data by default (User, Organization, Project, WorkItem, Sprint, Release, Team)
- `.With*()` fluent API unchanged; 513 tests pass with zero regressions

---

### Phase 2 — Authentication, Permissions & Team Management (2026-03-15)

#### Security Fixes (Phase 2.0)

- Secrets removed from VCS: `.env.example` added, `docker-compose.yml` refactored to `${VAR}` substitution, `appsettings.json` cleared
- `[Authorize]` added to `ApiControllerBase`; all 5 controllers inherit auth requirement
- SignalR `TeamFlowHub` validates `Project_View` permission before adding client to group; rejects invalid GUIDs
- Auth rate limit updated from 5/min to 10 per 15 min per IP with `Retry-After` header on 429
- Playwright installed with `playwright.config.ts`, page objects (`LoginPage`, `RegisterPage`), and smoke test

#### Authentication Backend (Phase 2.1)

- `RefreshToken` entity with `Token` (hashed), `ExpiresAt`, `RevokedAt`, `ReplacedByToken` fields; EF Core migration added
- `IAuthService` interface: `GenerateJwt`, `GenerateRefreshToken`, `HashPassword`, `VerifyPassword`
- `AuthService` implementation: JWT (HMAC-SHA256) + bcrypt password hashing
- 5 auth handlers: Register, Login, RefreshToken, ChangePassword, Logout
- `AuthController` at `/api/v1/auth` with `[AllowAnonymous]` on register/login/refresh; `[EnableRateLimiting(Auth)]` on all actions
- `JwtCurrentUser` replaces `FakeCurrentUser` in production; reads claims from `HttpContext.User`
- Refresh token rotation: old token revoked with `ReplacedByToken` set; expired/revoked token returns 401

#### Authentication Frontend (Phase 2.2)

- Auth Zustand store: user, tokens, login/logout/refresh actions
- Axios interceptor: on 401, attempts silent token refresh then retries original request; redirects to login on refresh failure
- Login and register pages with client-side validation and error handling
- Next.js middleware for protected route redirection
- SignalR connection passes JWT as `access_token` query parameter with reconnection support

#### Team Management (Phase 2.3)

- Team CRUD (create, update, delete, list, get) with `Team_Manage` permission enforcement
- Team member management: add, remove, change role (TeamManager scope-limited to own team)
- `TeamsController` at `/api/v1/teams` (8 endpoints)
- `ProjectMembershipsController` at `/api/v1/projects/{projectId}/memberships` (3 endpoints)
- `AddProjectMemberCommand` supports both User and Team member types
- `TeamRepository` and `ProjectMembershipRepository` registered in DI

#### Permission System (Phase 2.4)

- `PermissionChecker` implementation: 3-level resolution (Individual override → Team role → Org default)
- `PermissionMatrix` static class mapping all 6 roles to their 28 permissions
- OrgAdmin always returns true; no-membership returns false
- `RemoveWorkItemLinkHandler` fixed: explicit null check before permission check prevents Guid.Empty issue
- `ReorderBacklogHandler` fixed: validates each WorkItemId belongs to declared ProjectId before update

#### Permission-Aware Frontend (Phase 2.5)

- Permission Zustand store with `usePermission(permission)` hook
- `<Can permission="...">` component for conditional rendering
- Viewer role: create/edit/delete buttons hidden across all views
- Axios interceptor handles 403 with permission-denied toast notification
- Sidebar navigation filtered by role

#### Work Item History UI (Phase 2.6)

- `GetWorkItemHistoryQuery` handler: paginated history, newest-first, survives soft-delete of parent
- `GET /api/v1/workitems/{id}/history` endpoint added to `WorkItemsController`
- `HistoryTab` component with infinite scroll/load-more pagination
- `HistoryEntry` component: actor avatar, name, action description, relative timestamp, color-coded by action type
- Rejection reason displayed in history entry
- `history_added` SignalR event prepends new entries without full reload

---

### Phase 1 — Work Item Management (2026-03-15)

- Project CRUD (create, update, archive, delete, list with filter/search)
- Work item CRUD with hierarchy (Epic > Story > Task / Bug / Spike)
- Parent-child type enforcement and soft-delete cascade to children
- Work item status transitions with validation
- Single-assignee assignment with history tracking via append-only `WorkItemHistories`
- Item linking with 6 link types, bidirectional storage, circular blocking detection
- Release CRUD, item assignment with one-release-per-item constraint
- Backlog query: hierarchy-grouped, filtered by status/priority/assignee/type/sprint/release, searchable, reorderable
- Kanban board query: status-grouped columns, swimlanes by assignee or epic
- Realtime broadcast infrastructure: domain events published via MassTransit to RabbitMQ, consumed by background service, broadcast via SignalR hub
- Domain event persistence to event store table
- 124 tests: 17 domain, 99 application, 8 integration

### Phase 0 — Foundation (2026-03-15)

- .NET 10 solution (`TeamFlow.slnx`) with Clean Architecture + Vertical Slice layout
- 21 domain entities, 10 enums, 16 domain events across `TeamFlow.Domain`
- EF Core `TeamFlowDbContext` with 21 `DbSet<T>` properties and global soft-delete query filter (`deleted_at IS NULL`)
- Docker Compose: PostgreSQL 17, RabbitMQ 4 with management UI; health-checked dependency ordering
- API skeleton: versioning (`/api/v{version}/`), rate limiting (5 policies), health checks, Swagger/OpenAPI
- `ApiControllerBase` with `HandleResult<T>` mapping `Result<T>` errors to `ProblemDetails` responses
- SignalR hub registered and routed
- MassTransit configured against RabbitMQ with dead-letter queue support
- Quartz.NET scheduler wired into `TeamFlow.BackgroundServices`
- Test infrastructure: `IntegrationTestBase` with Testcontainers (real PostgreSQL), test data builders pattern established
