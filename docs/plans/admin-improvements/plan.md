# Plan: Admin Improvements

**Created:** 2026-03-16
**Feature:** Force password change, pagination/search, logout, admin password reset, org management, user/org activation
**Type:** Fullstack (Backend + Frontend + API)
**Discovery:** `docs/plans/admin-improvements/discovery-context.md`
**Base branch:** `feat/org-management-admin-bootstrap`

## Phase Status

| Phase | Name | Status |
|-------|------|--------|
| 0 | Version Control | completed |
| 1 | Entity Changes + Migration | completed |
| 2 | Research | completed |
| 3 | Planning | completed |
| 4a | API Contract | completed |
| 4b | Implementation | completed |
| 5 | Testing | completed |
| 6 | Documentation | completed |

## Success Criteria

- [x] Branch `feat/org-management-admin-bootstrap` confirmed current
- [x] Plan reviewed and approved
- [x] Research complete — all source files scouted, phase-2-summary.md written
- [x] API contract written to `docs/plans/admin-improvements/api-contract-260316-1200.md` with all 8 endpoints, full request/response JSON shapes, TypeScript interfaces
- [x] All 8 backend endpoints implemented with TFD (tests first)
- [x] Frontend: login redirect, change-password page, admin grids updated
- [x] All tests passing (1015 total: 73 domain + 745 application + 25 background + 141 api + 31 infrastructure)
- [x] `tsc --noEmit` passes clean (0 errors)

---

## Overview

Six features that harden the admin panel: forced password change on first login, paginated/searchable grids, logout, admin-initiated password resets, org name/owner management, and user/org deactivation. Six sequential phases, each with TFD on all backend work.

## Phase Dependencies

```
Phase 1 (Entity Changes + Migration)
    |
Phase 2 (Force Password Change — F1)
    |
Phase 3 (Admin Password Reset — F4)
    |
Phase 4 (Deactivate/Activate — F6)
    |
Phase 5 (Paging & Search — F2)
    |
Phase 6 (Admin UI — F3 + F5 + grid updates)
```

---

## API Contract

| Endpoint | Method | Auth | Phase | Feature |
|----------|--------|------|-------|---------|
| `POST /api/v1/auth/change-password` | POST | Authenticated | 2 | F1 (modify existing) |
| `POST /api/v1/admin/users/{userId}/reset-password` | POST | SystemAdmin | 3 | F4 |
| `PUT /api/v1/admin/users/{userId}/status` | PUT | SystemAdmin | 4 | F6 |
| `PUT /api/v1/admin/organizations/{orgId}/status` | PUT | SystemAdmin | 4 | F6 |
| `GET /api/v1/admin/users` | GET | SystemAdmin | 5 | F2 (add pagination+search) |
| `GET /api/v1/admin/organizations` | GET | SystemAdmin | 5 | F2 (add pagination+search) |
| `PUT /api/v1/admin/organizations/{orgId}` | PUT | SystemAdmin | 6 | F5 |
| `PUT /api/v1/admin/organizations/{orgId}/owner` | PUT | SystemAdmin | 6 | F5 |

---

## Phase 1: Entity Changes + Migration [S]

**Goal:** Add `MustChangePassword` and `IsActive` to User, `IsActive` to Organization. Generate EF migration. Seed admin with `MustChangePassword = true`.
**Dependencies:** None
**Estimated effort:** S

### FILE OWNERSHIP
- `src/core/TeamFlow.Domain/Entities/User.cs`
- `src/core/TeamFlow.Domain/Entities/Organization.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Services/AdminSeedService.cs`
- `src/core/TeamFlow.Infrastructure/Migrations/YYYYMMDD_AddAdminImprovementFields.cs`
- `tests/TeamFlow.Domain.Tests/Entities/UserTests.cs`
- `tests/TeamFlow.Tests.Common/Builders/UserBuilder.cs`
- `tests/TeamFlow.Tests.Common/Builders/OrganizationBuilder.cs`

### 1.1 Domain Changes [S]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Domain.Tests/Entities/UserTests.cs` | MODIFY: `MustChangePassword` defaults to `false`; `IsActive` defaults to `true` |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Domain/Entities/User.cs` | MODIFY | Add `public bool MustChangePassword { get; set; }` (default `false`); Add `public bool IsActive { get; set; } = true;` |
| `src/core/TeamFlow.Domain/Entities/Organization.cs` | MODIFY | Add `public bool IsActive { get; set; } = true;` |

### 1.2 Infrastructure Changes [S]

| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | MODIFY | Map `must_change_password` (bool, default false); Map `is_active` (bool, default true) |
| `src/core/TeamFlow.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs` | MODIFY | Map `is_active` (bool, default true) |
| `src/core/TeamFlow.Infrastructure/Services/AdminSeedService.cs` | MODIFY | Set `MustChangePassword = true` when creating new admin user |

### 1.3 Test Infrastructure [S]

| File | Action | Description |
|------|--------|-------------|
| `tests/TeamFlow.Tests.Common/Builders/UserBuilder.cs` | MODIFY | Add `WithMustChangePassword(bool)` and `WithIsActive(bool)` methods |
| `tests/TeamFlow.Tests.Common/Builders/OrganizationBuilder.cs` | MODIFY | Add `WithIsActive(bool)` method |

### 1.4 EF Migration [S]

| File | Action | Description |
|------|--------|-------------|
| Migration via `dotnet ef migrations add AddAdminImprovementFields` | CREATE | Add `must_change_password` (bool default false) + `is_active` (bool default true) to `users`; Add `is_active` (bool default true) to `organizations` |

**Phase 1 exit criteria:** `dotnet test` passes. New columns exist. Seed service sets `MustChangePassword = true`. Test builders updated.

---

## Phase 2: Force Password Change Flow (F1) [M]

**Goal:** When the seeded admin logs in for the first time, the API returns `mustChangePassword: true`. Frontend redirects to change-password page. Dismissing without changing logs out.
**Dependencies:** Phase 1 complete
**Estimated effort:** M

### FILE OWNERSHIP — Backend
- `src/core/TeamFlow.Application/Features/Auth/Login/LoginHandler.cs`
- `src/core/TeamFlow.Application/Features/Auth/AuthResponse.cs`
- `src/core/TeamFlow.Application/Features/Auth/ChangePassword/ChangePasswordHandler.cs`
- `tests/TeamFlow.Application.Tests/Features/Auth/LoginTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Auth/ChangePasswordTests.cs`

### FILE OWNERSHIP — Frontend
- `src/apps/teamflow-web/app/login/page.tsx`
- `src/apps/teamflow-web/app/admin/change-password/page.tsx`
- `src/apps/teamflow-web/lib/stores/auth-store.ts`
- `src/apps/teamflow-web/lib/api/types.ts`

### 2.1 Backend: Modify Login Response [S]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Auth/LoginTests.cs` | MODIFY: Add test `Handle_UserWithMustChangePassword_ReturnsFlag`; Verify `AuthResponse.MustChangePassword` is `true` when user flag is set |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Auth/AuthResponse.cs` | MODIFY | Add `bool MustChangePassword = false` to record (optional param with default preserves existing consumers) |
| `src/core/TeamFlow.Application/Features/Auth/Login/LoginHandler.cs` | MODIFY | After successful auth, set `MustChangePassword = user.MustChangePassword` in response |

### 2.2 Backend: Modify ChangePassword to Clear Flag [S]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Auth/ChangePasswordTests.cs` | MODIFY: Add test `Handle_UserWithMustChangePassword_ClearsFlag`; Verify user's `MustChangePassword` is set to `false` after successful password change |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Auth/ChangePassword/ChangePasswordHandler.cs` | MODIFY | After updating password hash, set `user.MustChangePassword = false` before `UpdateAsync` |

### 2.3 Frontend: Force Password Change UI [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/lib/api/types.ts` | MODIFY | Add `mustChangePassword?: boolean` to auth response type (the type used when calling login API) |
| `src/apps/teamflow-web/lib/stores/auth-store.ts` | MODIFY | Add `mustChangePassword: boolean` to `AuthState`; Set from login response; Clear on password change |
| `src/apps/teamflow-web/app/login/page.tsx` | MODIFY | After login, if `response.mustChangePassword`, redirect to `/admin/change-password` instead of `/admin` |
| `src/apps/teamflow-web/app/admin/change-password/page.tsx` | CREATE | Change password form. On success: clear `mustChangePassword` in store, redirect to `/admin`. On dismiss/navigate away: call `clearAuth()`, redirect to `/login`. Uses `beforeunload` and route change detection. |

**Phase 2 exit criteria:** `dotnet test` passes. Seeded admin login returns `mustChangePassword: true`. After password change, flag clears. Frontend forces change-password page. Dismissing logs out.

---

## Phase 3: Admin Password Reset (F4) [M]

**Goal:** SystemAdmin can reset any user's password. Sets `MustChangePassword = true` so user must change on next login.
**Dependencies:** Phase 2 complete (MustChangePassword flow works)
**Estimated effort:** M

### FILE OWNERSHIP
- `src/core/TeamFlow.Application/Features/Admin/ResetUserPassword/`
- `src/apps/TeamFlow.Api/Controllers/AdminController.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/ResetUserPasswordTests.cs`

### 3.1 Application Layer [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/ResetUserPasswordTests.cs` | CREATE: SystemAdmin can reset password; Non-SystemAdmin gets forbidden; User not found returns error; Password is hashed (not stored plain); MustChangePassword set to true; All refresh tokens revoked |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/ResetUserPassword/AdminResetUserPasswordCommand.cs` | CREATE | `sealed record AdminResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>` |
| `src/core/TeamFlow.Application/Features/Admin/ResetUserPassword/AdminResetUserPasswordHandler.cs` | CREATE | Check SystemAdmin; Find user; Hash password; Set `MustChangePassword = true`; Revoke all refresh tokens; Persist |
| `src/core/TeamFlow.Application/Features/Admin/ResetUserPassword/AdminResetUserPasswordValidator.cs` | CREATE | `NewPassword` not empty, min length 8 |

### 3.2 API Layer [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/AdminController.cs` | MODIFY | Add `POST users/{userId}/reset-password` endpoint |

**Phase 3 exit criteria:** `dotnet test` passes. Admin can reset any user's password. Target user's `MustChangePassword` is `true`. All target user's sessions revoked.

---

## Phase 4: Deactivate/Activate Users and Orgs (F6) [L]

**Goal:** SystemAdmin can toggle `IsActive` on users and organizations. Deactivated users cannot login or call APIs. Deactivated orgs block access. Org deactivation revokes pending invitations.
**Dependencies:** Phase 1 complete (IsActive fields exist)
**Estimated effort:** L

### FILE OWNERSHIP
- `src/core/TeamFlow.Application/Features/Admin/ChangeUserStatus/`
- `src/core/TeamFlow.Application/Features/Admin/ChangeOrgStatus/`
- `src/core/TeamFlow.Application/Common/Behaviors/ActiveUserBehavior.cs`
- `src/core/TeamFlow.Application/Features/Auth/Login/LoginHandler.cs` (shared with Phase 2)
- `src/core/TeamFlow.Application/Common/Interfaces/IInvitationRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/InvitationRepository.cs`
- `src/apps/TeamFlow.Api/Controllers/AdminController.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/ChangeUserStatusTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/ChangeOrgStatusTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Auth/LoginTests.cs` (add deactivation test)
- `tests/TeamFlow.Application.Tests/Common/ActiveUserBehaviorTests.cs`

### 4.1 Login Deactivation Check [S]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Auth/LoginTests.cs` | MODIFY: Add `Handle_DeactivatedUser_ReturnsForbidden` test |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Auth/Login/LoginHandler.cs` | MODIFY | After password verify, check `user.IsActive`. If false, return `DomainError.Forbidden("Your account has been deactivated")` |

### 4.2 MediatR Pipeline Behavior for Active User [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Common/ActiveUserBehaviorTests.cs` | CREATE: Active user passes through; Deactivated user gets forbidden; Unauthenticated request passes through (anonymous endpoints) |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Common/Behaviors/ActiveUserBehavior.cs` | CREATE | `sealed class ActiveUserBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`. Inject `ICurrentUser` and `IUserRepository`. For authenticated requests, load user, check `IsActive`. If false, short-circuit with forbidden. Skip for anonymous endpoints (where `ICurrentUser.IsAuthenticated == false`). |
| `src/apps/TeamFlow.Api/Program.cs` or `DependencyInjection.cs` | MODIFY | Register `ActiveUserBehavior` in MediatR pipeline |

### 4.3 Change User Status [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/ChangeUserStatusTests.cs` | CREATE: SystemAdmin can deactivate user; SystemAdmin can reactivate user; Non-SystemAdmin gets forbidden; Cannot deactivate self; Cannot deactivate last SystemAdmin; User not found returns error; Deactivation revokes all refresh tokens |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/ChangeUserStatus/AdminChangeUserStatusCommand.cs` | CREATE | `sealed record AdminChangeUserStatusCommand(Guid UserId, bool IsActive) : IRequest<Result>` |
| `src/core/TeamFlow.Application/Features/Admin/ChangeUserStatus/AdminChangeUserStatusHandler.cs` | CREATE | Check SystemAdmin; Prevent self-deactivation; Prevent last SystemAdmin deactivation; Set `IsActive`; If deactivating, revoke all refresh tokens; Persist |
| `src/core/TeamFlow.Application/Features/Admin/ChangeUserStatus/AdminChangeUserStatusValidator.cs` | CREATE | `UserId` not empty |

### 4.4 Change Org Status [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/ChangeOrgStatusTests.cs` | CREATE: SystemAdmin can deactivate org; SystemAdmin can reactivate org; Non-SystemAdmin gets forbidden; Org not found returns error; Deactivation revokes all pending invitations |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/ChangeOrgStatus/AdminChangeOrgStatusCommand.cs` | CREATE | `sealed record AdminChangeOrgStatusCommand(Guid OrgId, bool IsActive) : IRequest<Result>` |
| `src/core/TeamFlow.Application/Features/Admin/ChangeOrgStatus/AdminChangeOrgStatusHandler.cs` | CREATE | Check SystemAdmin; Find org; Set `IsActive`; If deactivating, revoke all pending invitations via new repo method; Persist |
| `src/core/TeamFlow.Application/Features/Admin/ChangeOrgStatus/AdminChangeOrgStatusValidator.cs` | CREATE | `OrgId` not empty |
| `src/core/TeamFlow.Application/Common/Interfaces/IInvitationRepository.cs` | MODIFY | Add `Task RevokePendingByOrgAsync(Guid organizationId, CancellationToken ct)` |
| `src/core/TeamFlow.Infrastructure/Repositories/InvitationRepository.cs` | MODIFY | Implement `RevokePendingByOrgAsync` -- batch update pending invitations to Revoked status |

### 4.5 Org Access Guard [S]

**TFD: Write tests first, then implement.**

This checks org `IsActive` when a user accesses org-scoped resources. Implemented via the existing `GetOrganizationBySlugHandler` and org-scoped handlers.

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Organizations/GetBySlug/GetOrganizationBySlugHandler.cs` | MODIFY | After loading org, check `org.IsActive`. If false, return `DomainError.Forbidden("Organization has been deactivated")` |

### 4.6 API Layer [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/AdminController.cs` | MODIFY | Add `PUT users/{userId}/status` and `PUT organizations/{orgId}/status` endpoints |

**Phase 4 exit criteria:** `dotnet test` passes. Deactivated users cannot login (403). Deactivated users with valid JWTs get blocked by pipeline behavior. Org deactivation revokes pending invites. Deactivated org access returns 403.

---

## Phase 5: Paging and Search (F2) [M]

**Goal:** Add pagination and search to admin users and organizations grids.
**Dependencies:** Phase 1 complete (entity changes may affect DTOs)
**Estimated effort:** M

### FILE OWNERSHIP
- `src/core/TeamFlow.Application/Features/Admin/ListUsers/`
- `src/core/TeamFlow.Application/Features/Admin/ListOrganizations/`
- `src/core/TeamFlow.Application/Common/Interfaces/IUserRepository.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/IOrganizationRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/UserRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/OrganizationRepository.cs`
- `src/apps/TeamFlow.Api/Controllers/AdminController.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/ListAdminUsersTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/ListAdminOrganizationsTests.cs`

### 5.1 Backend: Paginated Admin Users [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/ListAdminUsersTests.cs` | MODIFY: Add tests for pagination (page 1 returns correct count, page 2 returns remainder); Search by name matches; Search by email matches; Empty search returns all; Includes `IsActive` and `MustChangePassword` in DTO |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/ListUsers/AdminListUsersQuery.cs` | MODIFY | Add `string? Search`, `int Page = 1`, `int PageSize = 20` parameters |
| `src/core/TeamFlow.Application/Features/Admin/ListUsers/AdminListUsersHandler.cs` | MODIFY | Change return type to `Result<PagedResult<AdminUserDto>>`; Use new paginated repo method |
| `src/core/TeamFlow.Application/Features/Admin/ListUsers/AdminUserDto.cs` | MODIFY | Add `bool IsActive`, `bool MustChangePassword` fields |
| `src/core/TeamFlow.Application/Common/Interfaces/IUserRepository.cs` | MODIFY | Add `Task<(IEnumerable<User> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken ct)` |
| `src/core/TeamFlow.Infrastructure/Repositories/UserRepository.cs` | MODIFY | Implement paginated `ListAsync` with name/email search using `ILIKE` |

### 5.2 Backend: Paginated Admin Organizations [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/ListAdminOrganizationsTests.cs` | MODIFY: Add tests for pagination; Search by name matches; Search by slug matches; Includes `Slug`, `MemberCount`, `IsActive` in DTO |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/ListOrganizations/AdminListOrganizationsQuery.cs` | MODIFY | Add `string? Search`, `int Page = 1`, `int PageSize = 20` parameters |
| `src/core/TeamFlow.Application/Features/Admin/ListOrganizations/AdminListOrganizationsHandler.cs` | MODIFY | Change return type to `Result<PagedResult<AdminOrganizationDto>>`; Use new paginated repo method |
| `src/core/TeamFlow.Application/Features/Admin/ListOrganizations/AdminOrganizationDto.cs` | MODIFY | Add `string Slug`, `int MemberCount`, `bool IsActive` fields |
| `src/core/TeamFlow.Application/Common/Interfaces/IOrganizationRepository.cs` | MODIFY | Add `Task<(IEnumerable<Organization> Items, int TotalCount)> ListAllAsync(string? search, int page, int pageSize, CancellationToken ct)` (overload) |
| `src/core/TeamFlow.Infrastructure/Repositories/OrganizationRepository.cs` | MODIFY | Implement paginated `ListAllAsync` with name/slug search |

### 5.3 API Layer [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/AdminController.cs` | MODIFY | Update `ListOrganizations` and `ListUsers` to accept `[FromQuery]` params: `search`, `page`, `pageSize`; Update return type annotations |

**Phase 5 exit criteria:** `dotnet test` passes. Admin users/orgs endpoints accept pagination and search params. Results are paginated. Search filters by name/email/slug. DTOs include new fields.

---

## Phase 6: Admin UI Improvements (F3 + F5 + Grid Updates) [L]

**Goal:** Add logout to admin layout. Org name/slug editing + owner transfer. Update grids with pagination, search, action buttons for password reset (F4), status toggle (F6).
**Dependencies:** Phases 2-5 complete
**Estimated effort:** L

### FILE OWNERSHIP
This phase owns ALL files under `src/apps/teamflow-web/app/admin/` and admin-related components/hooks/api.

Backend files for F5 (org update/owner transfer):
- `src/core/TeamFlow.Application/Features/Admin/UpdateOrganization/`
- `src/core/TeamFlow.Application/Features/Admin/TransferOrgOwnership/`
- `src/apps/TeamFlow.Api/Controllers/AdminController.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/AdminUpdateOrgTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Admin/TransferOrgOwnershipTests.cs`

### 6.1 Backend: Admin Update Organization (F5) [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/AdminUpdateOrgTests.cs` | CREATE: SystemAdmin can update name/slug; Non-SystemAdmin gets forbidden; Org not found returns error; Duplicate slug returns conflict; Empty name returns validation error |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/UpdateOrganization/AdminUpdateOrgCommand.cs` | CREATE | `sealed record AdminUpdateOrgCommand(Guid OrgId, string Name, string Slug) : IRequest<Result>` |
| `src/core/TeamFlow.Application/Features/Admin/UpdateOrganization/AdminUpdateOrgHandler.cs` | CREATE | Check SystemAdmin; Find org; Check slug uniqueness (excluding self); Update name + slug; Persist |
| `src/core/TeamFlow.Application/Features/Admin/UpdateOrganization/AdminUpdateOrgValidator.cs` | CREATE | Name not empty, max 100; Slug not empty, max 50, regex pattern |

### 6.2 Backend: Transfer Org Ownership (F5) [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/TransferOrgOwnershipTests.cs` | CREATE: SystemAdmin can transfer ownership; Non-SystemAdmin gets forbidden; New owner must be org member; Cannot transfer to current owner; Org not found returns error |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Admin/TransferOrgOwnership/AdminTransferOrgOwnershipCommand.cs` | CREATE | `sealed record AdminTransferOrgOwnershipCommand(Guid OrgId, Guid NewOwnerUserId) : IRequest<Result>` |
| `src/core/TeamFlow.Application/Features/Admin/TransferOrgOwnership/AdminTransferOrgOwnershipHandler.cs` | CREATE | Check SystemAdmin; Find org; Find current owner membership; Find new owner membership (must be member); Demote current owner to Admin; Promote new owner to Owner; Persist both |

### 6.3 API Layer for F5 [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/AdminController.cs` | MODIFY | Add `PUT organizations/{orgId}` and `PUT organizations/{orgId}/owner` endpoints |

### 6.4 Frontend: Admin Logout (F3) [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/admin/layout.tsx` | MODIFY | Add logout button above "Back to App" in sidebar. On click: call `clearAuth()`, redirect to `/login` |

### 6.5 Frontend: Admin API & Hooks [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/lib/api/types.ts` | MODIFY | Update `AdminUserDto` to include `isActive`, `mustChangePassword`; Update `AdminOrganizationDto` to include `slug`, `memberCount`, `isActive`; Add request body types for new endpoints |
| `src/apps/teamflow-web/lib/api/admin.ts` | MODIFY | Update `getAdminUsers` and `getAdminOrganizations` to accept pagination+search params; Add `resetUserPassword()`, `changeUserStatus()`, `changeOrgStatus()`, `updateAdminOrg()`, `transferOrgOwnership()` functions |
| `src/apps/teamflow-web/lib/hooks/use-admin.ts` | CREATE | TanStack Query hooks: `useAdminUsers(params)`, `useAdminOrganizations(params)`, `useResetUserPassword()`, `useChangeUserStatus()`, `useChangeOrgStatus()`, `useAdminUpdateOrg()`, `useTransferOrgOwnership()` |

### 6.6 Frontend: Users Grid Update [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/admin/users/page.tsx` | REWRITE | Add search input; Add pagination controls; Add columns: IsActive (badge), MustChangePassword (indicator); Add action buttons per row: "Reset Password", "Deactivate/Activate" toggle |
| `src/apps/teamflow-web/components/admin/reset-password-dialog.tsx` | CREATE | Dialog with password input field + confirm button. Calls `resetUserPassword()` mutation |
| `src/apps/teamflow-web/components/admin/user-status-toggle.tsx` | CREATE | Confirm dialog for activate/deactivate. Calls `changeUserStatus()` mutation |
| `src/apps/teamflow-web/components/admin/pagination-controls.tsx` | CREATE | Reusable pagination component: page numbers, prev/next, page size selector |
| `src/apps/teamflow-web/components/admin/search-input.tsx` | CREATE | Debounced search input (300ms debounce) |

### 6.7 Frontend: Organizations Grid Update [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/admin/organizations/page.tsx` | REWRITE | Add search input; Add pagination controls; Add columns: Slug, MemberCount, IsActive (badge); Add action buttons per row: "Edit", "Transfer Ownership", "Deactivate/Activate" toggle |
| `src/apps/teamflow-web/components/admin/edit-org-dialog.tsx` | CREATE | Dialog with name + slug fields. Calls `updateAdminOrg()` mutation |
| `src/apps/teamflow-web/components/admin/transfer-ownership-dialog.tsx` | CREATE | Dialog showing org members, select new owner. Calls `transferOrgOwnership()` mutation. Needs to fetch org members list |

### 6.8 Frontend: Deactivated Account Error Page [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/deactivated/page.tsx` | CREATE | Static error page: "Your account has been deactivated. Contact your administrator." With link to `/login` |
| `src/apps/teamflow-web/lib/api/client.ts` | MODIFY | In axios interceptor, if 403 response contains "deactivated", call `clearAuth()` and redirect to `/deactivated` |

**Phase 6 exit criteria:** Admin layout has logout button. Users grid has search, pagination, reset password, and status toggle. Orgs grid has search, pagination, edit, transfer ownership, and status toggle. Deactivated users/orgs show proper error. `npm run build` passes.

---

## Planning Notes

### Codebase Observations
1. `ChangePasswordCommand/Handler` already exists -- Phase 2 modifies it to clear `MustChangePassword` flag
2. `AuthResponse` is a sealed record -- adding an optional parameter with default value is backward compatible
3. Admin handlers use string-based `DomainError.Forbidden()` -- follow existing pattern for new handlers
4. `PagedResult<T>` and `ListProjectsQuery` provide the exact pagination pattern to follow
5. `UserConfiguration` is NOT sealed (unlike `OrganizationConfiguration`) -- follow existing pattern
6. `IInvitationRepository` has no batch update method -- Phase 4 adds `RevokePendingByOrgAsync`
7. Frontend admin pages use inline styles (not CSS modules or Tailwind) -- follow existing pattern
8. Login page currently routes SystemAdmin to `/admin` unconditionally -- Phase 2 adds `mustChangePassword` check before redirect

### Migration Safety
- `must_change_password` defaults to `false` -- existing users unaffected
- `is_active` defaults to `true` -- existing users/orgs remain active
- No data loss. All changes are additive columns with safe defaults.

### ActiveUserBehavior Considerations
The MediatR pipeline behavior must handle the `Result<T>` return type generically. Since handlers return `Result<T>` or `Result`, the behavior needs to construct the correct failure type. Pattern: check if `TResponse` is `Result` or `Result<T>` and construct accordingly. Skip behavior for login/register commands (unauthenticated).

### Test Strategy
- All backend phases use TFD
- Application tests use NSubstitute mocks for repositories
- Theory patterns for validation edge cases (empty strings, null, too-long values)
- Frontend: manual testing + `npm run build` verification
