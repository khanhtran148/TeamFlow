# API Contract: Admin Improvements

**Feature:** Force password change, admin password reset, user/org deactivation, paging/search, org management
**Date:** 2026-03-16 12:00
**Base URL:** `/api/v1`
**Auth:** All endpoints require `Authorization: Bearer <access_token>` unless noted.
**Error format:** RFC 7807 `ProblemDetails` for all non-2xx responses.

---

## Shared Types

### AuthResponse

Returned by `POST /auth/login` and `POST /auth/refresh-token`.

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2026-03-16T13:00:00Z",
  "mustChangePassword": false
}
```

| Field | Type | Notes |
|-------|------|-------|
| `accessToken` | string | JWT bearer token |
| `refreshToken` | string | Opaque refresh token |
| `expiresAt` | string (ISO 8601 UTC) | Access token expiry |
| `mustChangePassword` | boolean | `true` if user must change password before proceeding. Default `false`. |

### PagedResult\<T\>

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### ProblemDetails (error)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Forbidden",
  "status": 403,
  "detail": "Your account has been deactivated."
}
```

---

## Endpoint Index

| # | Method | Path | Auth | Plan Phase | Feature |
|---|--------|------|------|------------|---------|
| 1 | `POST` | `/auth/change-password` | Authenticated | Phase 2 | F1 — Force password change |
| 2 | `POST` | `/admin/users/{userId}/reset-password` | SystemAdmin | Phase 3 | F4 — Admin password reset |
| 3 | `PUT` | `/admin/users/{userId}/status` | SystemAdmin | Phase 4 | F6 — Toggle user active |
| 4 | `PUT` | `/admin/organizations/{orgId}/status` | SystemAdmin | Phase 4 | F6 — Toggle org active |
| 5 | `GET` | `/admin/users` | SystemAdmin | Phase 5 | F2 — Paginated + searchable users |
| 6 | `GET` | `/admin/organizations` | SystemAdmin | Phase 5 | F2 — Paginated + searchable orgs |
| 7 | `PUT` | `/admin/organizations/{orgId}` | SystemAdmin | Phase 6 | F5 — Update org name/slug |
| 8 | `PUT` | `/admin/organizations/{orgId}/owner` | SystemAdmin | Phase 6 | F5 — Transfer org ownership |

---

## 1. Change Password (F1)

**Path:** `POST /api/v1/auth/change-password`
**Auth:** Bearer token (any authenticated user)
**Rate limit:** `write`
**Purpose:** User changes their own password. Clears `mustChangePassword` flag on success.

### Request

```json
{
  "currentPassword": "OldPass@123",
  "newPassword": "NewPass@456"
}
```

| Field | Type | Validation |
|-------|------|-----------|
| `currentPassword` | string | Required, non-empty |
| `newPassword` | string | Required, min 8 characters |

### Response — 204 No Content

Empty body on success.

### Error Responses

| Status | Condition | Detail |
|--------|-----------|--------|
| 400 | Validation failure (empty/short password) | Field-level errors in `errors` map |
| 401 | Not authenticated | — |
| 422 | Current password incorrect | `"Current password is incorrect."` |

### Notes

- After successful change, `mustChangePassword` is set to `false` on the user record.
- No logout occurs — existing sessions remain valid.
- Frontend should call `clearMustChangePassword()` on store after success.

---

## 2. Admin Reset User Password (F4)

**Path:** `POST /api/v1/admin/users/{userId}/reset-password`
**Auth:** Bearer token — SystemAdmin role required (enforced in handler)
**Rate limit:** `write`
**Purpose:** SystemAdmin sets a new password for any user. Forces user to change on next login.

### Path Parameters

| Param | Type | Notes |
|-------|------|-------|
| `userId` | UUID (string) | Target user's ID |

### Request

```json
{
  "newPassword": "TempPass@789"
}
```

| Field | Type | Validation |
|-------|------|-----------|
| `newPassword` | string | Required, min 8 characters |

### Response — 204 No Content

Empty body on success.

### Error Responses

| Status | Condition | Detail |
|--------|-----------|--------|
| 400 | Validation failure | `"newPassword must be at least 8 characters."` |
| 403 | Caller is not SystemAdmin | `"Forbidden."` |
| 404 | User not found | `"User not found."` |

### Notes

- Password is hashed; never stored as plaintext.
- Sets `mustChangePassword = true` on the target user.
- All refresh tokens for the target user are revoked on success (forces re-login).

---

## 3. Change User Status (F6)

**Path:** `PUT /api/v1/admin/users/{userId}/status`
**Auth:** Bearer token — SystemAdmin role required
**Rate limit:** `write`
**Purpose:** Activate or deactivate a user account.

### Path Parameters

| Param | Type | Notes |
|-------|------|-------|
| `userId` | UUID (string) | Target user's ID |

### Request

```json
{
  "isActive": false
}
```

| Field | Type | Validation |
|-------|------|-----------|
| `isActive` | boolean | Required |

### Response — 204 No Content

Empty body on success.

### Error Responses

| Status | Condition | Detail |
|--------|-----------|--------|
| 400 | Validation failure | `"userId is required."` |
| 403 | Not SystemAdmin | `"Forbidden."` |
| 403 | Attempt to deactivate self | `"Cannot deactivate your own account."` |
| 403 | Attempt to deactivate last SystemAdmin | `"Cannot deactivate the last system administrator."` |
| 404 | User not found | `"User not found."` |

### Notes

- Deactivating a user (`isActive: false`) revokes all their refresh tokens immediately.
- Deactivated users receive 403 on login with detail `"Your account has been deactivated."`.
- Deactivated users with a still-valid JWT are blocked by the `ActiveUserBehavior` MediatR pipeline.
- Reactivating a user (`isActive: true`) does not restore revoked tokens; user must re-login.

---

## 4. Change Organization Status (F6)

**Path:** `PUT /api/v1/admin/organizations/{orgId}/status`
**Auth:** Bearer token — SystemAdmin role required
**Rate limit:** `write`
**Purpose:** Activate or deactivate an organization.

### Path Parameters

| Param | Type | Notes |
|-------|------|-------|
| `orgId` | UUID (string) | Target organization's ID |

### Request

```json
{
  "isActive": false
}
```

| Field | Type | Validation |
|-------|------|-----------|
| `isActive` | boolean | Required |

### Response — 204 No Content

Empty body on success.

### Error Responses

| Status | Condition | Detail |
|--------|-----------|--------|
| 400 | Validation failure | `"orgId is required."` |
| 403 | Not SystemAdmin | `"Forbidden."` |
| 404 | Organization not found | `"Organization not found."` |

### Notes

- Deactivating an org (`isActive: false`) revokes all pending invitations for that org.
- Accessing a deactivated org via `GET /api/v1/organizations/{slug}` returns 403 with detail `"Organization has been deactivated."`.
- Reactivating does not restore revoked invitations.

---

## 5. List Admin Users — with Pagination and Search (F2)

**Path:** `GET /api/v1/admin/users`
**Auth:** Bearer token — SystemAdmin role required
**Rate limit:** `general`
**Purpose:** List all users with pagination and optional search.

### Query Parameters

| Param | Type | Default | Notes |
|-------|------|---------|-------|
| `search` | string? | — | Case-insensitive search on `name` OR `email` (ILIKE `%search%`) |
| `page` | integer | `1` | 1-based page number |
| `pageSize` | integer | `20` | Items per page; max `100` |

### Request

No body. Example: `GET /api/v1/admin/users?search=alice&page=1&pageSize=20`

### Response — 200 OK

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "alice@example.com",
      "name": "Alice Smith",
      "systemRole": "User",
      "createdAt": "2026-01-10T08:30:00Z",
      "isActive": true,
      "mustChangePassword": false
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### AdminUserDto

| Field | Type | Notes |
|-------|------|-------|
| `id` | UUID (string) | |
| `email` | string | |
| `name` | string | |
| `systemRole` | string enum | `"User"` \| `"SystemAdmin"` |
| `createdAt` | string (ISO 8601 UTC) | |
| `isActive` | boolean | New in Phase 5 |
| `mustChangePassword` | boolean | New in Phase 5 |

### Error Responses

| Status | Condition |
|--------|-----------|
| 403 | Not SystemAdmin |

---

## 6. List Admin Organizations — with Pagination and Search (F2)

**Path:** `GET /api/v1/admin/organizations`
**Auth:** Bearer token — SystemAdmin role required
**Rate limit:** `general`
**Purpose:** List all organizations with pagination and optional search.

### Query Parameters

| Param | Type | Default | Notes |
|-------|------|---------|-------|
| `search` | string? | — | Case-insensitive search on `name` OR `slug` (ILIKE `%search%`) |
| `page` | integer | `1` | 1-based page number |
| `pageSize` | integer | `20` | Items per page; max `100` |

### Request

No body. Example: `GET /api/v1/admin/organizations?search=team&page=2&pageSize=10`

### Response — 200 OK

```json
{
  "items": [
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "name": "TeamFlow Inc",
      "slug": "teamflow-inc",
      "memberCount": 12,
      "createdAt": "2026-01-05T10:00:00Z",
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### AdminOrganizationDto

| Field | Type | Notes |
|-------|------|-------|
| `id` | UUID (string) | |
| `name` | string | |
| `slug` | string | New in Phase 5 |
| `memberCount` | integer | Count of active org members. New in Phase 5 |
| `createdAt` | string (ISO 8601 UTC) | |
| `isActive` | boolean | New in Phase 5 |

### Error Responses

| Status | Condition |
|--------|-----------|
| 403 | Not SystemAdmin |

---

## 7. Update Organization (F5)

**Path:** `PUT /api/v1/admin/organizations/{orgId}`
**Auth:** Bearer token — SystemAdmin role required
**Rate limit:** `write`
**Purpose:** Update an organization's display name and URL slug.

### Path Parameters

| Param | Type | Notes |
|-------|------|-------|
| `orgId` | UUID (string) | Target organization's ID |

### Request

```json
{
  "name": "TeamFlow Inc Rebranded",
  "slug": "teamflow-rebrand"
}
```

| Field | Type | Validation |
|-------|------|-----------|
| `name` | string | Required, non-empty, max 100 characters |
| `slug` | string | Required, non-empty, max 50 characters, pattern `^[a-z0-9-]+$` |

### Response — 204 No Content

Empty body on success.

### Error Responses

| Status | Condition | Detail |
|--------|-----------|--------|
| 400 | Validation failure | Field-level errors |
| 403 | Not SystemAdmin | `"Forbidden."` |
| 404 | Organization not found | `"Organization not found."` |
| 409 | Slug already in use by another org | `"Slug 'teamflow-rebrand' is already in use."` |

### Notes

- Slug uniqueness check excludes the org being updated (slug can remain unchanged).
- Slug format: lowercase letters, digits, and hyphens only (`^[a-z0-9-]+$`).

---

## 8. Transfer Organization Ownership (F5)

**Path:** `PUT /api/v1/admin/organizations/{orgId}/owner`
**Auth:** Bearer token — SystemAdmin role required
**Rate limit:** `write`
**Purpose:** Transfer org ownership from the current owner to a different org member.

### Path Parameters

| Param | Type | Notes |
|-------|------|-------|
| `orgId` | UUID (string) | Target organization's ID |

### Request

```json
{
  "newOwnerUserId": "5fa85f64-5717-4562-b3fc-2c963f66afa8"
}
```

| Field | Type | Validation |
|-------|------|-----------|
| `newOwnerUserId` | UUID (string) | Required, non-empty |

### Response — 204 No Content

Empty body on success.

### Error Responses

| Status | Condition | Detail |
|--------|-----------|--------|
| 400 | Validation failure | `"newOwnerUserId is required."` |
| 400 | New owner is not an org member | `"The specified user is not a member of this organization."` |
| 400 | New owner is already the current owner | `"The specified user is already the owner of this organization."` |
| 403 | Not SystemAdmin | `"Forbidden."` |
| 404 | Organization not found | `"Organization not found."` |
| 404 | New owner user not found | `"User not found."` |

### Notes

- Current owner's membership role is demoted to `Admin`.
- New owner's membership role is promoted to `Owner`.
- Both membership updates are persisted atomically.

---

## Frontend Integration Notes

### Force Password Change Flow (Phase 2)

1. `POST /auth/login` response includes `mustChangePassword: true/false`
2. If `true`: store `mustChangePassword` in auth store, redirect to `/admin/change-password`
3. On that page:
   - Submit: `POST /auth/change-password` → on success, clear store flag, redirect to `/admin`
   - Dismiss / navigate away: call `clearAuth()`, redirect to `/login`
4. Guard other admin routes: if `mustChangePassword` is true in store, redirect to `/admin/change-password`

### Deactivated Account Flow (Phase 4)

1. `POST /auth/login` with deactivated account → 403 with detail `"Your account has been deactivated."`
2. Any API call by a deactivated user with a still-valid JWT → 403 (pipeline behavior)
3. Client axios interceptor: on 403 response containing `"deactivated"` in detail → `clearAuth()` + redirect to `/deactivated`
4. `/deactivated` page: static message with link back to `/login`

### Pagination Pattern (Phase 5)

Query params: `?page=1&pageSize=20&search=alice`
Response shape: `PagedResult<T>` (see Shared Types above)
Frontend: debounce search input 300ms before firing query.

---

## Shared TypeScript Interface Definitions (Frontend)

```typescript
// lib/api/types.ts additions/changes

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;          // ISO 8601 UTC
  mustChangePassword: boolean; // default false
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AdminUserDto {
  id: string;
  email: string;
  name: string;
  systemRole: 'User' | 'SystemAdmin';
  createdAt: string;
  isActive: boolean;          // Phase 5 addition
  mustChangePassword: boolean; // Phase 5 addition
}

export interface AdminOrganizationDto {
  id: string;
  name: string;
  slug: string;               // Phase 5 addition
  memberCount: number;        // Phase 5 addition
  createdAt: string;
  isActive: boolean;          // Phase 5 addition
}

// Request body types
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AdminResetPasswordRequest {
  newPassword: string;
}

export interface ChangeStatusRequest {
  isActive: boolean;
}

export interface AdminUpdateOrgRequest {
  name: string;
  slug: string;
}

export interface TransferOwnershipRequest {
  newOwnerUserId: string;
}

export interface AdminListParams {
  search?: string;
  page?: number;
  pageSize?: number;
}
```
