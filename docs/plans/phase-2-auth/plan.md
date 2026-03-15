# Phase 2 -- Authentication & Authorization

**Created:** 2026-03-15
**Scope:** Fullstack (Backend + Frontend + E2E)
**Estimated Duration:** 3 weeks
**Discovery:** [discovery-context.md](discovery-context.md)
**Spec:** `docs/process/phases.md` -- Phase 2

---

## Phase Overview

| Sub-Phase | Goal | Size | Dependencies |
|-----------|------|------|--------------|
| 2.0 | P1 Security Fixes + Playwright Setup | M | None |
| 2.1 | Authentication Backend | L | 2.0 |
| 2.2 | Authentication Frontend | M | 2.1 |
| 2.3 | Team Management (Backend + Frontend) | M | 2.1 |
| 2.4 | Permission System Backend | L | 2.1, 2.3 |
| 2.5 | Permission-Aware Frontend | M | 2.2, 2.4 |
| 2.6 | Work Item History UI | M | 2.2, 2.5 |
| 2.7 | E2E Tests with Playwright | L | 2.0, 2.2, 2.5, 2.6 |

```
2.0 ──→ 2.1 ──→ 2.2 ──────────→ 2.5 ──→ 2.6 ──→ 2.7
              ├──→ 2.3 ──→ 2.4 ──┘
              └──────────────────────────────────→ 2.7
```

Phases 2.2 and 2.3 can run in PARALLEL after 2.1 completes. Phase 2.4 depends on both 2.1 and 2.3. Phase 2.5 depends on both 2.2 and 2.4.

---

## Phase 2.0 -- P1 Security Fixes & Playwright Setup

**Goal:** Close all Critical/High security findings from Phase 1. Set up Playwright test infrastructure.

### 2.0.1 -- Move secrets out of VCS (S)

**Layer:** DevOps
**Constraint:** HUMAN-REVIEWED (secrets handling)

| Task | Files |
|------|-------|
| Create `.env.example` with placeholder values | `.env.example` (new) |
| Create `.env` with real dev values, add to `.gitignore` | `.env` (new), `.gitignore` |
| Refactor `docker-compose.yml` to use `${VAR}` substitution | `docker-compose.yml` |
| Remove JWT secret from `appsettings.json` (already empty -- verify) | `src/apps/TeamFlow.Api/appsettings.json` |
| Add `appsettings.Development.json` with dev-only JWT secret (gitignored) | `src/apps/TeamFlow.Api/appsettings.Development.json` (new) |
| Remove fallback default in `Program.cs` -- fail fast on missing secret | `src/apps/TeamFlow.Api/Program.cs` (line ~53) |

### 2.0.2 -- Add [Authorize] to all controllers (S)

**Layer:** Backend

| Task | Files |
|------|-------|
| Add `[Authorize]` to `ApiControllerBase` | `src/apps/TeamFlow.Api/Controllers/Base/ApiControllerBase.cs` |
| Add `[AllowAnonymous]` to future auth endpoints (login, register) | Applied in Phase 2.1 |
| Verify health endpoints remain unauthenticated | `src/apps/TeamFlow.Api/Program.cs` |

**Tests (TFD):**

| Test | File |
|------|------|
| Unauthenticated request to `/api/v1/projects` returns 401 | `tests/TeamFlow.Api.Tests/Security/AuthorizationTests.cs` (new) |
| Request with valid JWT returns 200 | Same file |

### 2.0.3 -- Validate SignalR group-join (S)

**Layer:** Backend
**Constraint:** HUMAN-REVIEWED (security boundary)

| Task | Files |
|------|-------|
| Inject `IPermissionChecker` into `TeamFlowHub` | `src/apps/TeamFlow.Api/Hubs/TeamFlowHub.cs` |
| Before `AddToGroupAsync`, parse ID and call `HasPermissionAsync` with `WorkItem_View` / `Project_View` | Same file |
| Reject with `HubException` if denied | Same file |

**Tests (TFD):**

| Test | File |
|------|------|
| JoinProject with valid membership succeeds | `tests/TeamFlow.Api.Tests/Hubs/TeamFlowHubTests.cs` (new) |
| JoinProject without membership throws HubException | Same file |

### 2.0.4 -- Update Auth rate limit to spec (S)

**Layer:** Backend

| Task | Files |
|------|-------|
| Change Auth policy from 5/min to 10 per 15 minutes per IP | `src/apps/TeamFlow.Api/RateLimiting/RateLimitPolicies.cs` |
| Add `Retry-After` header to 429 responses | Same file |

**Tests (TFD):**

| Test | File |
|------|------|
| 11th auth request within 15 min returns 429 with Retry-After | `tests/TeamFlow.Api.Tests/RateLimiting/AuthRateLimitTests.cs` (new) |

### 2.0.5 -- Playwright setup (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Install Playwright and `@playwright/test` | `src/apps/teamflow-web/package.json` |
| Create Playwright config | `src/apps/teamflow-web/playwright.config.ts` (new) |
| Create base test fixtures (authenticated user, API helpers) | `src/apps/teamflow-web/e2e/fixtures/auth.ts` (new) |
| Create page object models directory | `src/apps/teamflow-web/e2e/pages/` (new) |
| Add `e2e` script to package.json | `src/apps/teamflow-web/package.json` |
| Add `.gitignore` entries for Playwright artifacts | `src/apps/teamflow-web/.gitignore` |

---

## Phase 2.1 -- Authentication Backend

**Goal:** Full auth flow -- register, login, refresh, change password, logout. Replace `FakeCurrentUser`.

### API Contract

New controller: `AuthController` at `/api/v1/auth`

| Method | Route | Auth | Rate Limit | Request | Response |
|--------|-------|------|------------|---------|----------|
| POST | `/register` | Anonymous | Auth | `RegisterCommand` | 201 `AuthResponse` |
| POST | `/login` | Anonymous | Auth | `LoginCommand` | 200 `AuthResponse` |
| POST | `/refresh` | Anonymous | Auth | `RefreshTokenCommand` | 200 `AuthResponse` |
| POST | `/change-password` | Bearer | Write | `ChangePasswordCommand` | 200 |
| POST | `/logout` | Bearer | Write | `LogoutCommand` | 200 |

```
AuthResponse {
  accessToken: string,
  refreshToken: string,
  expiresAt: ISO8601
}
```

### 2.1.1 -- RefreshToken entity and migration (S)

**Layer:** Domain + Infrastructure

| Task | Files |
|------|-------|
| Create `RefreshToken` entity | `src/core/TeamFlow.Domain/Entities/RefreshToken.cs` (new) |
| Add `DbSet<RefreshToken>` and EF config | `src/core/TeamFlow.Infrastructure/Persistence/TeamFlowDbContext.cs` |
| Add EF configuration | `src/core/TeamFlow.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` (new) |
| Create migration | `src/core/TeamFlow.Infrastructure/Persistence/Migrations/` (new) |

RefreshToken fields: `Id`, `UserId`, `Token` (hashed), `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByToken`.

**Tests (TFD):**

| Test | File |
|------|------|
| RefreshToken persists and loads correctly | `tests/TeamFlow.Infrastructure.Tests/Persistence/RefreshTokenPersistenceTests.cs` (new) |

### 2.1.2 -- Auth interfaces and DTOs (S)

**Layer:** Application

| Task | Files |
|------|-------|
| Create `IAuthService` interface (GenerateJwt, GenerateRefreshToken, HashPassword, VerifyPassword) | `src/core/TeamFlow.Application/Common/Interfaces/IAuthService.cs` (new) |
| Create `IUserRepository` interface | `src/core/TeamFlow.Application/Common/Interfaces/IUserRepository.cs` (new) |
| Create `IRefreshTokenRepository` interface | `src/core/TeamFlow.Application/Common/Interfaces/IRefreshTokenRepository.cs` (new) |
| Create `AuthResponse` DTO | `src/core/TeamFlow.Application/Features/Auth/AuthResponse.cs` (new) |

### 2.1.3 -- Register handler (M)

**Layer:** Application + Infrastructure
**Constraint:** HUMAN-REVIEWED (password hashing)

| Task | Files |
|------|-------|
| Create `RegisterCommand` + `RegisterValidator` | `src/core/TeamFlow.Application/Features/Auth/Register/RegisterCommand.cs` (new) |
| Create `RegisterHandler` | `src/core/TeamFlow.Application/Features/Auth/Register/RegisterHandler.cs` (new) |
| Implement `UserRepository` | `src/core/TeamFlow.Infrastructure/Repositories/UserRepository.cs` (new) |
| Implement `AuthService` (JWT + bcrypt) | `src/core/TeamFlow.Infrastructure/Services/AuthService.cs` (new) |
| Add test builder: `RefreshTokenBuilder` | `tests/TeamFlow.Tests.Common/Builders/RefreshTokenBuilder.cs` (new) |

Business rules: Email unique. Password min 8 chars, 1 upper, 1 lower, 1 digit. Returns JWT + refresh token.

**Tests (TFD):**

| Test | File |
|------|------|
| Valid registration returns tokens | `tests/TeamFlow.Application.Tests/Features/Auth/RegisterTests.cs` (new) |
| Duplicate email returns ConflictError | Same file |
| Theory: empty email, empty password, short password all return validation error | Same file |

### 2.1.4 -- Login handler (M)

**Layer:** Application

| Task | Files |
|------|-------|
| Create `LoginCommand` + `LoginValidator` | `src/core/TeamFlow.Application/Features/Auth/Login/LoginCommand.cs` (new) |
| Create `LoginHandler` | `src/core/TeamFlow.Application/Features/Auth/Login/LoginHandler.cs` (new) |

Business rules: Email+password match. Returns JWT + refresh token. Invalid credentials return 401 (UnauthorizedError).

**Tests (TFD):**

| Test | File |
|------|------|
| Valid credentials return tokens | `tests/TeamFlow.Application.Tests/Features/Auth/LoginTests.cs` (new) |
| Wrong password returns UnauthorizedError | Same file |
| Non-existent email returns UnauthorizedError | Same file |
| Theory: empty email, empty password return validation error | Same file |

### 2.1.5 -- Refresh token handler (M)

**Layer:** Application

| Task | Files |
|------|-------|
| Create `RefreshTokenCommand` + `RefreshTokenHandler` | `src/core/TeamFlow.Application/Features/Auth/RefreshToken/RefreshTokenCommand.cs` (new) |
| Implement `RefreshTokenRepository` | `src/core/TeamFlow.Infrastructure/Repositories/RefreshTokenRepository.cs` (new) |

Business rules: Valid non-revoked, non-expired token returns new JWT + new refresh token. Old refresh token is revoked with `ReplacedByToken` set. Expired/revoked token returns UnauthorizedError.

**Tests (TFD):**

| Test | File |
|------|------|
| Valid refresh token returns new tokens | `tests/TeamFlow.Application.Tests/Features/Auth/RefreshTokenTests.cs` (new) |
| Expired refresh token returns UnauthorizedError | Same file |
| Revoked refresh token returns UnauthorizedError | Same file |

### 2.1.6 -- Change password handler (S)

**Layer:** Application

| Task | Files |
|------|-------|
| Create `ChangePasswordCommand` + handler | `src/core/TeamFlow.Application/Features/Auth/ChangePassword/` (new) |

Business rules: Current password must match. New password follows same validation rules as register.

**Tests (TFD):**

| Test | File |
|------|------|
| Correct current password changes successfully | `tests/TeamFlow.Application.Tests/Features/Auth/ChangePasswordTests.cs` (new) |
| Wrong current password returns error | Same file |

### 2.1.7 -- Logout handler (S)

**Layer:** Application

| Task | Files |
|------|-------|
| Create `LogoutCommand` + handler | `src/core/TeamFlow.Application/Features/Auth/Logout/` (new) |

Business rules: Revoke all active refresh tokens for the user.

**Tests (TFD):**

| Test | File |
|------|------|
| Logout revokes all refresh tokens for user | `tests/TeamFlow.Application.Tests/Features/Auth/LogoutTests.cs` (new) |

### 2.1.8 -- AuthController and CurrentUser replacement (M)

**Layer:** Api

| Task | Files |
|------|-------|
| Create `AuthController` with `[AllowAnonymous]` on register/login/refresh | `src/apps/TeamFlow.Api/Controllers/AuthController.cs` (new) |
| Apply `[EnableRateLimiting(RateLimitPolicies.Auth)]` to auth endpoints | Same file |
| Create `JwtCurrentUser` that reads claims from `HttpContext.User` | `src/apps/TeamFlow.Api/Services/JwtCurrentUser.cs` (new) |
| Replace `FakeCurrentUser` registration with `JwtCurrentUser` in `Program.cs` | `src/apps/TeamFlow.Api/Program.cs` |
| Keep `FakeCurrentUser` available for tests | No deletion |

**Tests (TFD):**

| Test | File |
|------|------|
| POST /register with valid body returns 201 + tokens | `tests/TeamFlow.Api.Tests/Auth/AuthControllerTests.cs` (new) |
| POST /login with valid credentials returns 200 + tokens | Same file |
| POST /refresh with valid token returns 200 + new tokens | Same file |
| POST /change-password with auth returns 200 | Same file |
| POST /logout with auth returns 200 | Same file |
| Register -> Login -> call /projects with JWT returns 200 | Same file |

---

## Phase 2.2 -- Authentication Frontend

**Goal:** Login/register pages, JWT storage, Axios interceptor, protected routes, logout.

**PARALLEL:** No (sequential after 2.1)

### 2.2.1 -- Auth store and token management (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Create auth Zustand store (user, tokens, login/logout/refresh actions) | `src/apps/teamflow-web/lib/stores/auth-store.ts` (new) |
| Create auth API functions (register, login, refresh, changePassword, logout) | `src/apps/teamflow-web/lib/api/auth.ts` (new) |
| Add auth types to API types | `src/apps/teamflow-web/lib/api/types.ts` |

Token storage: `httpOnly` cookies. API sets `Set-Cookie` headers on login/refresh/logout. Frontend stores no tokens — Axios sends cookies automatically via `withCredentials: true`. Add CSRF token header for mutation requests.

### 2.2.2 -- Axios JWT interceptor (S)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Uncomment and implement JWT request interceptor | `src/apps/teamflow-web/lib/api/client.ts` |
| Add response interceptor: on 401, attempt silent refresh, retry original request | Same file |
| On refresh failure, redirect to login | Same file |

### 2.2.3 -- Login and Register pages (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Create login page | `src/apps/teamflow-web/app/login/page.tsx` (new) |
| Create register page | `src/apps/teamflow-web/app/register/page.tsx` (new) |
| Create shared auth form components | `src/apps/teamflow-web/components/auth/` (new) |
| Add form validation (client-side) | Same directory |
| Handle error responses (duplicate email, invalid credentials) | Same directory |

### 2.2.4 -- Protected route middleware (S)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Create auth middleware (redirect unauthenticated users to /login) | `src/apps/teamflow-web/middleware.ts` (new) |
| Update layout to show user info in TopBar | `src/apps/teamflow-web/app/layout.tsx` |
| Add logout button to TopBar | `src/apps/teamflow-web/components/layout/topbar.tsx` |

### 2.2.5 -- SignalR auth integration (S)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Pass JWT as `access_token` query param on SignalR connection | `src/apps/teamflow-web/lib/signalr/connection.ts` |
| Handle SignalR reconnection with fresh token | Same file |

---

## Phase 2.3 -- Team Management (Backend + Frontend)

**Goal:** CRUD teams, manage members, assign Team Manager role.

**PARALLEL:** Yes (with Phase 2.2)

### API Contract

New controller: `TeamsController` at `/api/v1/teams`

| Method | Route | Permission | Request | Response |
|--------|-------|------------|---------|----------|
| POST | `/` | Team_Manage | `CreateTeamCommand` | 201 `TeamDto` |
| GET | `/` | - (list own teams) | query: `orgId` | 200 `TeamDto[]` |
| GET | `/{id}` | - | - | 200 `TeamDto` |
| PUT | `/{id}` | Team_Manage | `UpdateTeamBody` | 200 `TeamDto` |
| DELETE | `/{id}` | Team_Manage | - | 204 |
| POST | `/{id}/members` | Team_Manage | `AddMemberBody` | 200 |
| DELETE | `/{id}/members/{userId}` | Team_Manage | - | 204 |
| PUT | `/{id}/members/{userId}/role` | Team_Manage | `ChangeRoleBody` | 200 |

New controller: `ProjectMembershipsController` at `/api/v1/projects/{projectId}/memberships`

| Method | Route | Permission | Request | Response |
|--------|-------|------------|---------|----------|
| POST | `/` | Project_ManageMembers | `AddProjectMemberCommand` | 200 |
| DELETE | `/{membershipId}` | Project_ManageMembers | - | 204 |
| GET | `/` | Project_View | - | 200 `ProjectMembershipDto[]` |

### FILE OWNERSHIP -- Backend

| Owner | Files |
|-------|-------|
| Phase 2.3 | `src/core/TeamFlow.Application/Features/Teams/**` |
| Phase 2.3 | `src/core/TeamFlow.Application/Features/ProjectMemberships/**` |
| Phase 2.3 | `src/core/TeamFlow.Application/Common/Interfaces/ITeamRepository.cs` |
| Phase 2.3 | `src/core/TeamFlow.Application/Common/Interfaces/IProjectMembershipRepository.cs` |
| Phase 2.3 | `src/core/TeamFlow.Infrastructure/Repositories/TeamRepository.cs` |
| Phase 2.3 | `src/core/TeamFlow.Infrastructure/Repositories/ProjectMembershipRepository.cs` |
| Phase 2.3 | `src/apps/TeamFlow.Api/Controllers/TeamsController.cs` |
| Phase 2.3 | `src/apps/TeamFlow.Api/Controllers/ProjectMembershipsController.cs` |
| Phase 2.3 | `tests/TeamFlow.Application.Tests/Features/Teams/**` |
| Phase 2.3 | `tests/TeamFlow.Api.Tests/Teams/**` |

### FILE OWNERSHIP -- Frontend

| Owner | Files |
|-------|-------|
| Phase 2.3 | `src/apps/teamflow-web/app/teams/**` |
| Phase 2.3 | `src/apps/teamflow-web/lib/api/teams.ts` |
| Phase 2.3 | `src/apps/teamflow-web/lib/hooks/use-teams.ts` |
| Phase 2.3 | `src/apps/teamflow-web/components/teams/**` |

### 2.3.1 -- Team repository and interfaces (S)

**Layer:** Application + Infrastructure

| Task | Files |
|------|-------|
| Create `ITeamRepository` | `src/core/TeamFlow.Application/Common/Interfaces/ITeamRepository.cs` (new) |
| Create `IProjectMembershipRepository` | `src/core/TeamFlow.Application/Common/Interfaces/IProjectMembershipRepository.cs` (new) |
| Implement `TeamRepository` | `src/core/TeamFlow.Infrastructure/Repositories/TeamRepository.cs` (new) |
| Implement `ProjectMembershipRepository` | `src/core/TeamFlow.Infrastructure/Repositories/ProjectMembershipRepository.cs` (new) |
| Add test builders: `TeamBuilder`, `TeamMemberBuilder` | `tests/TeamFlow.Tests.Common/Builders/TeamBuilder.cs` (new), `TeamMemberBuilder.cs` (new) |
| Register repositories in DI | `src/core/TeamFlow.Infrastructure/DependencyInjection.cs` |

### 2.3.2 -- Team CRUD handlers (M)

**Layer:** Application

| Task | Files |
|------|-------|
| `CreateTeamCommand` + handler + validator | `src/core/TeamFlow.Application/Features/Teams/CreateTeam/` (new) |
| `UpdateTeamCommand` + handler + validator | `src/core/TeamFlow.Application/Features/Teams/UpdateTeam/` (new) |
| `DeleteTeamCommand` + handler | `src/core/TeamFlow.Application/Features/Teams/DeleteTeam/` (new) |
| `ListTeamsQuery` + handler | `src/core/TeamFlow.Application/Features/Teams/ListTeams/` (new) |
| `GetTeamQuery` + handler | `src/core/TeamFlow.Application/Features/Teams/GetTeam/` (new) |
| `TeamDto` | `src/core/TeamFlow.Application/Features/Teams/TeamDto.cs` (new) |

Business rules: Team Manager can manage own team only. Org Admin can manage all teams.

**Tests (TFD):**

| Test | File |
|------|------|
| Create team with valid data returns TeamDto | `tests/TeamFlow.Application.Tests/Features/Teams/CreateTeamTests.cs` (new) |
| Theory: empty name returns validation error | Same file |
| Non-team-manager cannot create team | Same file |
| List returns only teams for the specified org | `tests/TeamFlow.Application.Tests/Features/Teams/ListTeamsTests.cs` (new) |

### 2.3.3 -- Team member management handlers (M)

**Layer:** Application

| Task | Files |
|------|-------|
| `AddTeamMemberCommand` + handler + validator | `src/core/TeamFlow.Application/Features/Teams/AddMember/` (new) |
| `RemoveTeamMemberCommand` + handler | `src/core/TeamFlow.Application/Features/Teams/RemoveMember/` (new) |
| `ChangeTeamMemberRoleCommand` + handler | `src/core/TeamFlow.Application/Features/Teams/ChangeRole/` (new) |

Business rules: Team Manager manages own team only. Adding a user who is already a member returns ConflictError.

**Tests (TFD):**

| Test | File |
|------|------|
| Add member to own team succeeds | `tests/TeamFlow.Application.Tests/Features/Teams/AddMemberTests.cs` (new) |
| Add member to another manager's team returns ForbiddenError | Same file |
| Duplicate member returns ConflictError | Same file |
| Team Manager manages own team success; other team 403 | `tests/TeamFlow.Application.Tests/Features/Teams/TeamManagerScopeTests.cs` (new) |

### 2.3.4 -- Project membership handlers (M)

**Layer:** Application

| Task | Files |
|------|-------|
| `AddProjectMemberCommand` + handler (supports MemberType: User or Team) | `src/core/TeamFlow.Application/Features/ProjectMemberships/AddMember/` (new) |
| `RemoveProjectMemberCommand` + handler | `src/core/TeamFlow.Application/Features/ProjectMemberships/RemoveMember/` (new) |
| `ListProjectMembershipsQuery` + handler | `src/core/TeamFlow.Application/Features/ProjectMemberships/ListMembers/` (new) |
| `ProjectMembershipDto` | `src/core/TeamFlow.Application/Features/ProjectMemberships/ProjectMembershipDto.cs` (new) |

**Tests (TFD):**

| Test | File |
|------|------|
| Add user to project with role succeeds | `tests/TeamFlow.Application.Tests/Features/ProjectMemberships/AddMemberTests.cs` (new) |
| Add team to project with role succeeds | Same file |
| Duplicate membership returns ConflictError | Same file |

### 2.3.5 -- Teams and memberships controllers (S)

**Layer:** Api

| Task | Files |
|------|-------|
| Create `TeamsController` | `src/apps/TeamFlow.Api/Controllers/TeamsController.cs` (new) |
| Create `ProjectMembershipsController` | `src/apps/TeamFlow.Api/Controllers/ProjectMembershipsController.cs` (new) |

**Tests (TFD):**

| Test | File |
|------|------|
| Full CRUD team through API | `tests/TeamFlow.Api.Tests/Teams/TeamLifecycleTests.cs` (new) |
| Add/remove member through API | `tests/TeamFlow.Api.Tests/Teams/TeamMemberTests.cs` (new) |

### 2.3.6 -- Team management frontend (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Team API client functions | `src/apps/teamflow-web/lib/api/teams.ts` (new) |
| TanStack Query hooks for teams | `src/apps/teamflow-web/lib/hooks/use-teams.ts` (new) |
| Teams list page | `src/apps/teamflow-web/app/teams/page.tsx` (new) |
| Team detail page with member management | `src/apps/teamflow-web/app/teams/[teamId]/page.tsx` (new) |
| Team member list/add/remove components | `src/apps/teamflow-web/components/teams/` (new) |
| Role assignment dropdown | Same directory |

---

## Phase 2.4 -- Permission System Backend

**Goal:** Replace `AlwaysAllowPermissionChecker` with real 3-level resolution. Enforce on all Phase 1 endpoints.

**Constraint:** HUMAN-REVIEWED -- Permission resolution logic must be reviewed by human before merge.

### 2.4.1 -- Permission resolution implementation (L)

**Layer:** Infrastructure
**Constraint:** HUMAN-REVIEWED

| Task | Files |
|------|-------|
| Create `PermissionChecker` implementing `IPermissionChecker` | `src/core/TeamFlow.Infrastructure/Services/PermissionChecker.cs` (new) |
| Create `PermissionMatrix` static class mapping Role -> Permission[] | `src/core/TeamFlow.Application/Common/Authorization/PermissionMatrix.cs` (new) |
| Implement 3-level resolution: Individual (CustomPermissions) -> Team -> Organization | `PermissionChecker.cs` |
| Org Admin always returns true | Same file |
| Replace `AlwaysAllowPermissionChecker` registration with `PermissionChecker` in `Program.cs` | `src/apps/TeamFlow.Api/Program.cs` |

Resolution logic:
1. If user is Org Admin at org level, return true.
2. Check `project_memberships` where `member_type = 'User'` and `member_id = userId` for individual override.
3. If individual membership found, check `custom_permissions` JSONB first, then role's default permissions.
4. Check `project_memberships` where `member_type = 'Team'` and team contains user.
5. Fall back to org-level default role (Viewer if no membership at all returns false).

**Tests (TFD):**

| Test | File |
|------|------|
| Org Admin always has permission | `tests/TeamFlow.Infrastructure.Tests/Services/PermissionCheckerTests.cs` (new) |
| Developer with individual Tech Lead override on project resolves correctly | Same file |
| Team member inherits team's project role | Same file |
| User with no membership returns false | Same file |
| Individual override takes precedence over team role | Same file |
| Theory: all 6 roles tested against permission matrix | Same file |

### 2.4.2 -- Permission matrix definition (M)

**Layer:** Application

| Task | Files |
|------|-------|
| Map all 28 permissions to each of the 6 roles based on `docs/product/roles-permissions.md` | `src/core/TeamFlow.Application/Common/Authorization/PermissionMatrix.cs` (new) |

**Tests (TFD):**

| Test | File |
|------|------|
| OrgAdmin has all permissions | `tests/TeamFlow.Application.Tests/Authorization/PermissionMatrixTests.cs` (new) |
| Viewer has only View permissions | Same file |
| Developer cannot delete projects | Same file |
| PO cannot start sprints | Same file |
| Tech Lead can close tasks | Same file |
| Theory: exhaustive matrix spot-checks from roles-permissions.md | Same file |

### 2.4.3 -- Enforce permissions on all Phase 1 handlers (M)

**Layer:** Application

Verify every mutating handler calls `HasPermissionAsync`. The following are already implemented:
- All WorkItem handlers (Create, Update, Delete, ChangeStatus, Move, Assign, Unassign, AddLink, RemoveLink)
- All Project handlers (Create, Update, Delete, Archive)
- All Release handlers

Verify the correct permission enum is used for each handler:

| Handler | Expected Permission |
|---------|-------------------|
| CreateWorkItem | WorkItem_Create |
| UpdateWorkItem | WorkItem_Edit |
| DeleteWorkItem | WorkItem_Delete |
| ChangeWorkItemStatus | WorkItem_ChangeStatus |
| MoveWorkItem | WorkItem_Edit |
| AssignWorkItem | WorkItem_AssignOther |
| UnassignWorkItem | WorkItem_AssignOther |
| AddWorkItemLink | WorkItem_ManageLinks |
| RemoveWorkItemLink | WorkItem_ManageLinks |
| CreateProject | Org_Admin |
| UpdateProject | Project_Edit |
| DeleteProject | Project_Edit |
| ArchiveProject | Project_Archive |
| CreateRelease | Release_Create |
| UpdateRelease | Release_Edit |
| DeleteRelease | Release_Edit |
| AssignItemToRelease | Release_Edit |
| RemoveItemFromRelease | Release_Edit |
| ReorderBacklog | WorkItem_Edit |

**Tests (TFD):**

| Test | File |
|------|------|
| Viewer calls POST /workitems returns 403 | `tests/TeamFlow.Api.Tests/Security/PermissionEnforcementTests.cs` (new) |
| Developer calls DELETE /projects/{id} returns 403 | Same file |
| Org Admin never receives 403 | Same file |
| Theory: each role tested against representative endpoints | Same file |

### 2.4.4 -- Fix RemoveLink Guid.Empty issue (S)

**Layer:** Application

| Task | Files |
|------|-------|
| Fail explicitly if source item is null before permission check | `src/core/TeamFlow.Application/Features/WorkItems/RemoveLink/RemoveWorkItemLinkHandler.cs` |

**Tests (TFD):**

| Test | File |
|------|------|
| RemoveLink with deleted source item returns NotFoundError | `tests/TeamFlow.Application.Tests/Features/WorkItems/RemoveLinkPermissionTests.cs` (new) |

### 2.4.5 -- Fix ReorderBacklog cross-project issue (S)

**Layer:** Application

| Task | Files |
|------|-------|
| Validate each WorkItemId belongs to the declared ProjectId before updating | `src/core/TeamFlow.Application/Features/Backlog/ReorderBacklog/ReorderBacklogHandler.cs` |

**Tests (TFD):**

| Test | File |
|------|------|
| Reorder with foreign WorkItemId returns error | `tests/TeamFlow.Application.Tests/Features/Backlog/ReorderBacklogSecurityTests.cs` (new) |

---

## Phase 2.5 -- Permission-Aware Frontend

**Goal:** Conditional UI rendering based on user permissions. Handle 403 responses.

### 2.5.1 -- Permission API and context (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Create API function: GET `/api/v1/projects/{id}/memberships/me` returning current user's effective role + permissions | Backend: add endpoint to `ProjectMembershipsController`; Frontend: `src/apps/teamflow-web/lib/api/permissions.ts` (new) |
| Create permission Zustand store | `src/apps/teamflow-web/lib/stores/permission-store.ts` (new) |
| Create `usePermission(permission)` hook | `src/apps/teamflow-web/lib/hooks/use-permission.ts` (new) |
| Create `<Can permission="WorkItem_Create">` component | `src/apps/teamflow-web/components/auth/can.tsx` (new) |

### 2.5.2 -- Conditional UI rendering (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Hide create/edit/delete buttons for Viewer role | Multiple component files in `components/` |
| Hide project management actions for non-admin roles | `src/apps/teamflow-web/app/projects/page.tsx` |
| Disable vote button for PO in refinement (placeholder for Phase 4) | N/A -- document only |
| Show/hide based on permissions in backlog and board views | Backlog and board page files |

### 2.5.3 -- 403 error handling (S)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Add 403 handler to Axios response interceptor (show permission denied toast) | `src/apps/teamflow-web/lib/api/client.ts` |
| Create ForbiddenPage component | `src/apps/teamflow-web/components/errors/forbidden.tsx` (new) |
| Update navigation to reflect role-based menu items | `src/apps/teamflow-web/components/layout/sidebar.tsx` |

---

## Phase 2.6 -- Work Item History UI

**Goal:** History tab in Work Item Detail with chronological feed, pagination, and realtime updates.

### API Contract

New endpoint on existing WorkItems controller:

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `/api/v1/workitems/{id}/history` | Bearer | Paginated `WorkItemHistoryDto[]` |

Query params: `page`, `pageSize` (default 20).

```
WorkItemHistoryDto {
  id: uuid,
  actorId: uuid,
  actorName: string,
  actorType: "User" | "System" | "AI",
  actionType: string,
  fieldName: string | null,
  oldValue: string | null,
  newValue: string | null,
  metadata: object,
  createdAt: ISO8601
}
```

### 2.6.1 -- History query handler (S)

**Layer:** Application

| Task | Files |
|------|-------|
| Create `GetWorkItemHistoryQuery` + handler | `src/core/TeamFlow.Application/Features/WorkItems/GetHistory/` (new) |
| Create `WorkItemHistoryDto` | Same directory |
| Add `IWorkItemHistoryRepository` (or extend existing) with paged query | `src/core/TeamFlow.Application/Common/Interfaces/IWorkItemHistoryRepository.cs` (new) |
| Implement repository | `src/core/TeamFlow.Infrastructure/Repositories/WorkItemHistoryRepository.cs` (new) |

Business rules: Returns history newest-first. Includes history for soft-deleted items. No mutation endpoints for history -- read-only.

**Tests (TFD):**

| Test | File |
|------|------|
| Returns history ordered newest first | `tests/TeamFlow.Application.Tests/Features/WorkItems/GetHistoryTests.cs` (new) |
| Pagination works correctly at 500+ entries | Same file |
| History survives soft-delete of parent item | Same file |
| No endpoint can modify history records | `tests/TeamFlow.Api.Tests/Security/HistoryImmutabilityTests.cs` (new) |

### 2.6.2 -- History API endpoint (S)

**Layer:** Api

| Task | Files |
|------|-------|
| Add `GetHistory` action to `WorkItemsController` | `src/apps/TeamFlow.Api/Controllers/WorkItemsController.cs` |

### 2.6.3 -- History tab frontend (M)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Create history API functions | `src/apps/teamflow-web/lib/api/work-items.ts` (extend) |
| Create history TanStack Query hook | `src/apps/teamflow-web/lib/hooks/use-work-item-history.ts` (new) |
| Create HistoryTab component | `src/apps/teamflow-web/components/work-items/history-tab.tsx` (new) |
| Create HistoryEntry component (avatar, name, action, relative time) | `src/apps/teamflow-web/components/work-items/history-entry.tsx` (new) |
| Visual distinction by action type (color coding/icons) | Same component |
| Rejection reason display in history entry | Same component |
| Pagination (infinite scroll or load-more) | `history-tab.tsx` |
| Add History tab to Work Item Detail page | `src/apps/teamflow-web/app/projects/[projectId]/work-items/[workItemId]/page.tsx` |

### 2.6.4 -- Realtime history updates (S)

**Layer:** Frontend

| Task | Files |
|------|-------|
| Listen for `history_added` SignalR event on workitem group | `src/apps/teamflow-web/lib/signalr/event-handlers.ts` |
| Prepend new history entry to the feed without full reload | `history-tab.tsx` |
| Invalidate TanStack Query cache on history event | Same file |

---

## Phase 2.7 -- E2E Tests with Playwright

**Goal:** Full acceptance criteria coverage. Every AC from the Phase 2 spec has at least one E2E test.

### 2.7.1 -- Auth flow E2E tests (M)

**Layer:** E2E

| Test | AC | File |
|------|-----|------|
| Register -> Login -> JWT -> call protected API -> success | AC1 | `src/apps/teamflow-web/e2e/auth/auth-flow.spec.ts` (new) |
| Token expires + valid refresh -> new token, no logout | AC2 | Same file |
| 11th auth request in 15 min -> 429 with Retry-After | AC8 | `src/apps/teamflow-web/e2e/auth/rate-limit.spec.ts` (new) |

### 2.7.2 -- Permission denial E2E tests (M)

**Layer:** E2E

| Test | AC | File |
|------|-----|------|
| Viewer calls POST /workitems -> 403 | AC3 | `src/apps/teamflow-web/e2e/permissions/permission-denial.spec.ts` (new) |
| Developer deletes Project -> 403 | AC4 | Same file |
| Org Admin never receives 403 (test representative operations) | AC7 | Same file |
| Individual override: Developer with Tech Lead on one project resolves correctly | AC6 | `src/apps/teamflow-web/e2e/permissions/individual-override.spec.ts` (new) |

### 2.7.3 -- Team management E2E tests (M)

**Layer:** E2E

| Test | AC | File |
|------|-----|------|
| Team Manager manages own team -> success | AC5 | `src/apps/teamflow-web/e2e/teams/team-management.spec.ts` (new) |
| Team Manager manages other team -> 403 | AC5 | Same file |

### 2.7.4 -- Permission-aware UI E2E tests (M)

**Layer:** E2E

| Test | AC | File |
|------|-----|------|
| PO has no vote button in refinement | AC9 | `src/apps/teamflow-web/e2e/permissions/role-ui.spec.ts` (new) |
| Tech Lead can close Task, can flag Story | AC10 | Same file |

### 2.7.5 -- History UI E2E tests (M)

**Layer:** E2E

| Test | AC | File |
|------|-----|------|
| Every mutation generates exactly one history record | AC11 | `src/apps/teamflow-web/e2e/history/history-tracking.spec.ts` (new) |
| History survives soft-delete of parent item | AC12 | Same file |
| No history modifiable via any endpoint including Org Admin | AC13 | Same file |

---

## Cross-Cutting Concerns

### New Test Builders Needed

| Builder | File |
|---------|------|
| `RefreshTokenBuilder` | `tests/TeamFlow.Tests.Common/Builders/RefreshTokenBuilder.cs` |
| `TeamBuilder` | `tests/TeamFlow.Tests.Common/Builders/TeamBuilder.cs` |
| `TeamMemberBuilder` | `tests/TeamFlow.Tests.Common/Builders/TeamMemberBuilder.cs` |

### Domain Events to Add

| Event | Phase | File |
|-------|-------|------|
| `UserRegisteredDomainEvent` | 2.1 | `src/core/TeamFlow.Domain/Events/AuthDomainEvents.cs` (new) |
| `TeamCreatedDomainEvent` | 2.3 | `src/core/TeamFlow.Domain/Events/TeamDomainEvents.cs` (new) |
| `TeamMemberAddedDomainEvent` | 2.3 | Same file |
| `TeamMemberRemovedDomainEvent` | 2.3 | Same file |
| `ProjectMembershipAddedDomainEvent` | 2.3 | Same file |

### Files Modified Across Multiple Phases

These files are touched by multiple phases. Changes must be merged carefully:

| File | Phases | Risk |
|------|--------|------|
| `Program.cs` | 2.0, 2.1, 2.4 | Low (distinct sections) |
| `docker-compose.yml` | 2.0 only | Low |
| `WorkItemsController.cs` | 2.6 only | Low (add one action) |
| `client.ts` (Axios) | 2.2, 2.5 | Medium (same interceptor area) |

---

## Assumptions

1. The `User` entity already has `PasswordHash` field -- no migration needed for user table.
2. `Team`, `TeamMember`, `ProjectMembership` entities already exist with correct schema -- no structural migration needed for these tables.
3. Only `RefreshToken` table requires a new migration.
4. The auth rate limit spec says "10 req/15 min per IP" -- this is per-IP, not per-user.
5. PO "no vote button in refinement" (AC9) -- Planning Poker is Phase 4. The E2E test will verify the UI hides the button based on role, using a stub refinement view if needed.
6. Tech Lead "can close Task, can flag Story" (AC10) -- these are status changes on existing Phase 1 endpoints, tested via permission checks.

## Decisions (Resolved)

1. **Token storage:** httpOnly cookies. API sets `Set-Cookie` on login/refresh. Frontend never touches tokens directly. CSRF protection required.
2. **No-membership default:** Deny all. User with no project/team membership gets no access (PermissionChecker returns false).
3. **AC9 (PO vote button):** Create a minimal stub refinement page in Phase 2 with a vote button, enough to verify PO cannot see it. Full refinement UI deferred to Phase 4.
