# Implement State: Admin Improvements

## Topic
Implement 6 admin improvement features: force password change, paging/search, logout, admin password reset, org management, deactivate/activate.

## Discovery Context

- **Branch:** `feat/org-management-admin-bootstrap` (continue on current)
- **Feature Scope:** Fullstack
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)

## Requirements

### F1: Force Password Change on First Admin Login
- `MustChangePassword` bool on User entity
- Seeded admin gets `MustChangePassword = true`
- Login response includes `mustChangePassword` flag
- Frontend: if flag is true, redirect to `/admin/change-password`; dismiss = logout
- Backend: `POST /api/v1/auth/change-password` (old + new password), clears the flag

### F2: Paging and Search for Admin Grids
- Admin users grid: pagination (page/pageSize) + search by name/email
- Admin orgs grid: pagination + search by name/slug
- Update existing handlers to accept params
- Follow existing pagination pattern from codebase

### F3: Logout on Admin Page
- Logout button in admin layout
- Uses existing clearAuth() + redirect to /login

### F4: Admin Password Reset
- `POST /api/v1/admin/users/{userId}/reset-password` (SystemAdmin only)
- Admin enters new password for the user
- Sets `MustChangePassword = true` so user must change on next login
- Frontend: "Reset Password" button on user row

### F5: Update Org Name + Change Owner
- `PUT /api/v1/admin/organizations/{orgId}` (update name/slug)
- `PUT /api/v1/admin/organizations/{orgId}/owner` (transfer ownership)
- Frontend: edit dialog + transfer ownership dialog

### F6: Deactivate/Activate Users and Orgs
- `IsActive` bool on User and Organization entities (default true)
- `PUT /api/v1/admin/users/{userId}/status` { isActive: bool }
- `PUT /api/v1/admin/organizations/{orgId}/status` { isActive: bool }
- Deactivated user login returns 403 "Account deactivated"
- Deactivated user API access returns 403 via MediatR pipeline behavior
- Deactivated org access returns 403
- Revoke pending invitations when org deactivated
- Frontend: toggle buttons on grids, error page for deactivated accounts

### Coding Conventions (from CLAUDE.md)
- All new classes sealed
- Primary constructors
- Result<T> pattern
- FluentValidation
- ProblemDetails for errors
- TFD: tests first for all backend
- xUnit + FluentAssertions + NSubstitute
- Theory patterns with InlineData

## Phase-Specific Context

- **Plan directory:** docs/plans/admin-improvements
- **Plan source:** docs/plans/admin-improvements/plan.md
- **Previous work:** Phases 1-6 of org management already implemented on this branch (944 tests passing)

## Current Progress (Plan Phases)

### Plan Phase 1: Entity Changes + Migration — COMPLETE
- User entity: `MustChangePassword` (bool, default false), `IsActive` (bool, default true)
- Organization entity: `IsActive` (bool, default true)
- EF configurations mapped with snake_case columns
- AdminSeedService sets `MustChangePassword = true` for new admin
- Migration `20260316110912_AddAdminImprovementFields` created
- UserBuilder: `WithMustChangePassword()`, `WithIsActive()` added
- OrganizationBuilder: `WithIsActive()` added
- Domain tests in `UserTests.cs` verify defaults
- 948 tests passing

### Plan Phase 2: Force Password Change (F1) — BACKEND COMPLETE, FRONTEND PARTIAL
- Backend DONE:
  - `AuthResponse` record: added `bool MustChangePassword = false` parameter
  - `LoginHandler`: passes `user.MustChangePassword` to AuthResponse
  - `ChangePasswordHandler`: sets `user.MustChangePassword = false` after password change
  - Tests: `Handle_UserWithMustChangePassword_ReturnsFlag`, `Handle_UserWithoutMustChangePassword_ReturnsFlagFalse`, `Handle_UserWithMustChangePassword_ClearsFlag` — all passing
- Frontend PARTIAL:
  - `lib/api/auth.ts`: `AuthResponse` type updated with `mustChangePassword?: boolean`
  - `lib/stores/auth-store.ts`: `mustChangePassword` state + `clearMustChangePassword()` added (PARTIAL — setAuth updated but changes need verification)
  - NOT DONE: Login page redirect logic, change-password page creation

### Plan Phase 3: Admin Password Reset (F4) — COMPLETE
- `AdminResetUserPasswordCommand`, `AdminResetUserPasswordHandler`, `AdminResetUserPasswordValidator` created
- Tests: 11 tests in `ResetUserPasswordTests.cs` — all passing (TFD)
- Controller endpoint `POST /admin/users/{userId}/reset-password` added

### Plan Phase 4: Deactivate/Activate (F6) — COMPLETE
- Login deactivation check added to `LoginHandler`
- `ActiveUserBehavior` MediatR pipeline behavior created and registered
- `AdminChangeUserStatusCommand/Handler/Validator` created
- `AdminChangeOrgStatusCommand/Handler/Validator` created
- `IInvitationRepository.RevokePendingByOrgAsync` added and implemented
- `GetOrganizationBySlugHandler` updated with IsActive guard
- Controller endpoints: `PUT /admin/users/{userId}/status`, `PUT /admin/organizations/{orgId}/status`
- Tests: 23 tests (ChangeUserStatusTests + ChangeOrgStatusTests + ActiveUserBehaviorTests) — all passing (TFD)

### Plan Phase 5: Paging and Search (F2) — COMPLETE
- `AdminListUsersQuery` updated with Search/Page/PageSize params, returns `PagedResult<AdminUserDto>`
- `AdminListOrganizationsQuery` updated similarly, returns `PagedResult<AdminOrganizationDto>`
- `AdminUserDto` extended with `IsActive`, `MustChangePassword`
- `AdminOrganizationDto` extended with `Slug`, `MemberCount`, `IsActive`
- `IUserRepository.ListPagedAsync` and `IOrganizationRepository.ListAllPagedAsync` added and implemented
- Tests: 15 tests (ListAdminUsersPagedTests + ListAdminOrganizationsPagedTests) — all passing (TFD)

### Plan Phase 6 (backend only): AdminUpdateOrg + TransferOrgOwnership (F5) — COMPLETE
- `AdminUpdateOrgCommand/Handler/Validator` created
- `AdminTransferOrgOwnershipCommand/Handler/Validator` created
- Controller endpoints: `PUT /admin/organizations/{orgId}`, `PUT /admin/organizations/{orgId}/owner`
- Tests: 21 tests (AdminUpdateOrgTests + TransferOrgOwnershipTests) — all passing (TFD)
- Total tests: 1015 passing (was 948)

### Plan Phase 6 (frontend) — NOT STARTED
