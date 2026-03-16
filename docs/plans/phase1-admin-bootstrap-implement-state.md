# Implement State: Phase 1 — System Admin Bootstrap

## Topic
Implement Phase 1 (System Admin Bootstrap) of the org management feature, plus admin frontend dashboard from Phase 4.6.

## Discovery Context

- **Branch:** `feat/org-management-admin-bootstrap`
- **Feature Scope:** Fullstack (backend + frontend)
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)

## Requirements

### Backend (Phase 1 from plan)
1. **Domain:** `SystemRole` enum (User | SystemAdmin), add to User entity
2. **Infrastructure:** `AdminSeedService : IHostedService` — reads `SystemAdmin:Email` and `SystemAdmin:Password` from appsettings.json, creates user if not exists, promotes if exists. Idempotent.
3. **Infrastructure:** Add `system_role` column mapping to UserConfiguration. Add `system_role` claim to JWT.
4. **Application:** Admin handlers — `AdminListOrganizationsQuery/Handler`, `AdminListUsersQuery/Handler`. Check `SystemRole == SystemAdmin`. Return all orgs/users.
5. **Application:** Add `SystemRole` to `ICurrentUser` interface
6. **API:** `AdminController` with `GET /admin/organizations` and `GET /admin/users`
7. **API:** Parse `system_role` claim in `JwtCurrentUser`
8. **API:** Register `AdminSeedService` in Program.cs
9. **Migration:** Add `system_role` int column to `users` table, default 0
10. **Test infra:** Add `SystemRole` to test stubs and builders

### Frontend (Phase 4.6 from plan)
1. **Auth store:** Add `systemRole` to auth user, parse from JWT
2. **Admin guard:** Component checking `systemRole === 'SystemAdmin'`
3. **Admin layout:** `/admin/layout.tsx` with admin guard
4. **Admin dashboard:** `/admin/page.tsx` — overview
5. **Admin orgs page:** `/admin/organizations/page.tsx` — list all organizations
6. **Admin users page:** `/admin/users/page.tsx` — list all users

### TFD Workflow (Non-Negotiable)
1. Write failing tests FIRST
2. Implement minimal code to make tests pass
3. Refactor while green

### Coding Conventions (from CLAUDE.md)
- All new classes MUST be sealed
- Primary constructors for services
- Result<T> pattern (CSharpFunctionalExtensions)
- FluentValidation
- ProblemDetails for errors
- xUnit + FluentAssertions + NSubstitute
- Theory pattern with InlineData (no separate Facts per value)
- File-scoped namespaces
- No "Arrange/Act/Assert" comments
- Test data builders, never raw construction

## Phase-Specific Context

- **Plan directory:** docs/plans/org-management-admin-bootstrap
- **Plan source:** docs/plans/org-management-admin-bootstrap/plan.md
- **ADR:** docs/adrs/260316-org-management-admin-bootstrap.md
- **API contract:** docs/plans/org-management-admin-bootstrap/api-contract-260316-1500.md

## Key Files to Read Before Implementing
- `src/core/TeamFlow.Domain/Entities/User.cs`
- `src/core/TeamFlow.Domain/Enums/` (existing enum patterns)
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Services/AuthService.cs` (JWT generation)
- `src/core/TeamFlow.Application/Common/Interfaces/ICurrentUser.cs`
- `src/apps/TeamFlow.Api/Services/JwtCurrentUser.cs`
- `src/apps/TeamFlow.Api/Program.cs`
- `src/apps/TeamFlow.Api/Controllers/` (controller patterns)
- `tests/TeamFlow.Tests.Common/` (test infrastructure patterns)
- `src/apps/teamflow-web/lib/stores/auth-store.ts`
- `src/apps/teamflow-web/lib/utils/jwt.ts` (JWT parsing)
