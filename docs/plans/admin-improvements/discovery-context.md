# Discovery Context: Admin Improvements

**Date:** 2026-03-16
**Branch:** `feat/admin-improvements`
**Base:** `feat/org-management-admin-bootstrap`

---

## Codebase State

### Existing Entities
- `User` (sealed, extends `BaseEntity`): `Email`, `PasswordHash`, `Name`, `SystemRole` -- no `MustChangePassword`, no `IsActive`
- `Organization` (sealed, extends `BaseEntity`): `Name`, `Slug`, `CreatedByUserId` -- no `IsActive`
- `BaseEntity`: `Id` (Guid), `CreatedAt`, `UpdatedAt`

### Existing Auth Flow
- `LoginHandler`: validates credentials, returns JWT + refresh token. No deactivation check. No `MustChangePassword` check.
- `AuthService.GenerateJwt()`: includes `system_role` claim. No `must_change_password` claim.
- `ChangePasswordCommand/Handler`: exists at `POST /api/v1/auth/change-password`. Takes `CurrentPassword` + `NewPassword`. Revokes all sessions after change. Does NOT clear `MustChangePassword` flag (flag doesn't exist yet).
- Frontend login: after login, redirects SystemAdmin to `/admin`, others to `/onboarding`. No forced password change redirect.

### Existing Admin Layer
- `AdminController`: two endpoints -- `GET /admin/organizations`, `GET /admin/users`
- Both handlers (`AdminListOrganizationsHandler`, `AdminListUsersHandler`): check `SystemRole == SystemAdmin`, return full unfiltered lists. No pagination, no search.
- `AdminUserDto`: `Id`, `Email`, `Name`, `SystemRole`, `CreatedAt`
- `AdminOrganizationDto`: `Id`, `Name`, `CreatedAt` (missing `Slug`, `MemberCount`, `IsActive`)
- Admin seed (`AdminSeedService`): creates user with `SystemRole.SystemAdmin` from config. Does NOT set `MustChangePassword = true`.

### Existing Pagination Pattern
- `PagedResult<T>` record: `Items`, `TotalCount`, `Page`, `PageSize`, computed `TotalPages`, `HasNextPage`, `HasPreviousPage`
- `ListProjectsQuery`: accepts `OrgId?`, `Status?`, `Search?`, `Page`, `PageSize` -> returns `Result<PagedResult<ProjectDto>>`
- Repository does the filtering + pagination at DB level, returns `(items, totalCount)` tuple

### Existing Frontend Admin
- Admin layout: sidebar with Dashboard/Organizations/Users links, "Back to App" link. No logout button.
- Users page: flat table, no pagination, no search, no action buttons
- Orgs page: flat table, no pagination, no search, no action buttons
- `auth-store.ts`: `clearAuth()` clears all state. `parseJwtUser()` parses JWT claims.
- `PaginatedResponse<T>` type exists in `types.ts`

### Soft Delete Pattern
- `WorkItem` and `Comment` use `deleted_at` (nullable timestamptz) for soft delete
- No existing `IsActive` pattern on User or Organization -- this is a new concept

### Error Handling
- `DomainError` static class bridges typed errors to `Result<T>` string failures
- `ApiControllerBase.MapStringError()` uses content-based matching: "not found" -> 404, "forbidden" -> 403, "already exists" -> 409
- Key: new error messages must contain the right keywords for HTTP mapping

### Invitation Repository
- `IInvitationRepository.ListByOrgAsync()`: returns all invitations for an org (all statuses)
- `IInvitationRepository.ListPendingByEmailAsync()`: returns pending, non-expired invitations by email
- No batch status update method -- need to add for org deactivation (revoke all pending invitations)

---

## Design Decisions

### D1: MustChangePassword as Domain Property (not JWT claim)
The `MustChangePassword` flag lives on `User` entity and is returned in the login response. The frontend checks this flag post-login and redirects accordingly. Storing in JWT would require re-issuing tokens after password change.

### D2: IsActive vs Soft Delete
`IsActive` is distinct from soft delete. A deactivated user/org still exists and can be reactivated. `deleted_at` is permanent. `IsActive` is a boolean column with default `true`.

### D3: Login-time Enforcement
Both `MustChangePassword` and `IsActive` are checked in `LoginHandler`. The login response includes `mustChangePassword` so the frontend can redirect. Deactivated users get a 403 before receiving tokens.

### D4: API-level Middleware for Deactivated Users
Rather than checking `IsActive` in every handler, add a middleware/behavior that checks user active status for authenticated requests. This catches deactivated users who still hold valid JWTs.

### D5: Admin Password Reset Generates Specific Password
Admin provides the new password (not auto-generated). Sets `MustChangePassword = true` so the user must change on next login.

### D6: Org Deactivation Side Effects
When an org is deactivated: revoke all pending invitations for that org. Existing members retain their membership records but cannot access the org.
