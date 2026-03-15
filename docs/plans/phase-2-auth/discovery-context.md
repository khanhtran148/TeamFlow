# Discovery Context -- Phase 2 Auth & Authorization

## Scope

Fullstack: .NET 10 backend + Next.js frontend + Playwright E2E.

## Success Criteria

All Phase 2 acceptance criteria from `docs/process/phases.md` pass, with Playwright E2E coverage for every AC.

## Current State (Phase 1 complete)

### Backend

- JWT config exists in `Program.cs` (JwtBearer setup with `TokenValidationParameters`). Secret is empty in `appsettings.json` but hardcoded in `docker-compose.yml`.
- `ICurrentUser` interface: `Id`, `Email`, `Name`, `IsAuthenticated`.
- `FakeCurrentUser` stub returns seed user -- needs replacement with JWT claims resolver.
- `IPermissionChecker` with `HasPermissionAsync` and `GetEffectiveRoleAsync`.
- `AlwaysAllowPermissionChecker` stub -- needs real implementation.
- `Permission` enum: 28 values covering WorkItems, Sprints, Releases, Retro, Projects, Teams, Org.
- All four Project mutation handlers now have permission checks (CreateProject checks `Org_Admin`; Update/Delete/Archive check `Project_Edit`/`Project_Archive`).
- All WorkItem mutation handlers have permission checks.
- No `[Authorize]` attribute on any controller except the SignalR hub.
- SignalR hub group-join methods do not validate membership.
- Rate limiting policies defined: Auth (5/min), Write (30/min), Search (20/min), BulkAction (5/min), General (100/min). Auth policy needs updating to match spec (10 req/15 min per IP).
- Entities exist: User (with PasswordHash), Team, TeamMember (with Role), ProjectMembership (with MemberType, Role, CustomPermissions).
- No RefreshToken entity yet.
- Test infra: `IntegrationTestBase` with Testcontainers, 8 test builders.

### Frontend

- Next.js 16 + TypeScript + Tailwind CSS v4 + shadcn/ui.
- Axios client with correlation ID. JWT interceptor skeleton (commented out).
- TanStack Query + Zustand. Providers wired.
- Layout shell: sidebar, topbar, theme toggle.
- Projects CRUD pages, backlog, board, releases, work item detail.
- SignalR client with connection lifecycle.
- No auth pages, no auth store, no protected routes.

### Security Issues from Phase 1 Review

1. JWT secret hardcoded in `docker-compose.yml` line 57 (Critical).
2. DB/RabbitMQ passwords hardcoded in `docker-compose.yml` (High).
3. No `[Authorize]` on controllers (Critical).
4. SignalR group-join does not validate membership (High).
5. Program.cs line 53 has no fail-fast on missing JWT secret -- currently empty string in appsettings, would throw on startup.
6. Auth rate limit policy is 5/min, spec requires 10/15min.

## Architecture Constraints

- Permission resolution logic is HUMAN-REVIEWED (Claude scaffolds, human writes resolution).
- JWT generation/validation is HUMAN-REVIEWED.
- All classes sealed by default.
- TFD: tests first, then implementation.
- All handlers return `Result<T>` via CSharpFunctionalExtensions.
- All API errors return ProblemDetails (RFC 7807).
- Permission checks through `IPermissionChecker` only.
- `WorkItemHistories` append-only -- no UPDATE/DELETE ever.

## No Existing Playwright Setup

Playwright must be set up from scratch for the frontend project.
