# API Contract: Org Management & Admin Bootstrap

**Date:** 2026-03-16
**Version:** 1.0
**Base:** `/api/v1`

---

## Phase 1: Admin Endpoints

### GET /admin/organizations
**Auth:** SystemAdmin only
**Response 200:**
```json
[
  {
    "id": "uuid",
    "name": "string",
    "slug": "string",
    "memberCount": 0,
    "createdAt": "2026-03-16T09:00:00Z"
  }
]
```
**Response 403:** ProblemDetails (non-SystemAdmin)

### GET /admin/users
**Auth:** SystemAdmin only
**Response 200:**
```json
[
  {
    "id": "uuid",
    "email": "string",
    "name": "string",
    "systemRole": "User | SystemAdmin",
    "createdAt": "2026-03-16T09:00:00Z",
    "organizationCount": 0
  }
]
```
**Response 403:** ProblemDetails (non-SystemAdmin)

---

## Phase 2: Organization CRUD

### POST /organizations
**Auth:** SystemAdmin only
**Request:**
```json
{
  "name": "string (1-100 chars)",
  "slug": "string (optional, auto-generated from name if omitted, 3-50 chars, lowercase alphanumeric + hyphens)"
}
```
**Response 201:**
```json
{
  "id": "uuid",
  "name": "string",
  "slug": "string",
  "createdAt": "2026-03-16T09:00:00Z"
}
```
**Response 400:** ProblemDetails (validation)
**Response 403:** ProblemDetails (non-SystemAdmin)
**Response 409:** ProblemDetails (slug already exists)

### PUT /organizations/{id}
**Auth:** Org Owner or Admin
**Request:**
```json
{
  "name": "string (1-100 chars)",
  "slug": "string (3-50 chars)"
}
```
**Response 200:** OrganizationDto
**Response 403:** ProblemDetails
**Response 404:** ProblemDetails
**Response 409:** ProblemDetails (slug conflict)

### GET /organizations/by-slug/{slug}
**Auth:** Org Member
**Response 200:**
```json
{
  "id": "uuid",
  "name": "string",
  "slug": "string",
  "createdAt": "2026-03-16T09:00:00Z"
}
```
**Response 403:** ProblemDetails (not a member)
**Response 404:** ProblemDetails

### GET /me/organizations
**Auth:** Authenticated
**Response 200:**
```json
[
  {
    "id": "uuid",
    "name": "string",
    "slug": "string",
    "role": "Owner | Admin | Member",
    "joinedAt": "2026-03-16T09:00:00Z"
  }
]
```

---

## Phase 3: Invitations

### POST /organizations/{orgId}/invitations
**Auth:** Org Owner or Admin
**Request:**
```json
{
  "email": "string (optional)",
  "role": "Admin | Member"
}
```
**Response 201:**
```json
{
  "id": "uuid",
  "token": "string (raw token, shown only on creation)",
  "inviteUrl": "string (frontend URL with token)",
  "role": "Admin | Member",
  "expiresAt": "2026-03-23T09:00:00Z",
  "status": "Pending"
}
```
**Notes:** Cannot invite as Owner (only one Owner per org). Token returned only in creation response.

### GET /organizations/{orgId}/invitations
**Auth:** Org Owner or Admin
**Response 200:**
```json
[
  {
    "id": "uuid",
    "email": "string | null",
    "role": "Admin | Member",
    "status": "Pending | Accepted | Expired | Revoked",
    "createdAt": "2026-03-16T09:00:00Z",
    "expiresAt": "2026-03-23T09:00:00Z",
    "acceptedByUserName": "string | null"
  }
]
```

### POST /invitations/{token}/accept
**Auth:** Authenticated
**Request:** Empty body (token in URL path)
**Response 200:**
```json
{
  "organizationId": "uuid",
  "organizationSlug": "string",
  "role": "Admin | Member"
}
```
**Response 400:** ProblemDetails (expired, already accepted, revoked)
**Response 404:** ProblemDetails (invalid token)

### DELETE /invitations/{id}
**Auth:** Org Owner or Admin (of the invitation's org)
**Response 204:** No content
**Response 400:** ProblemDetails (already accepted)
**Response 403:** ProblemDetails
**Response 404:** ProblemDetails

---

## Phase 5: Onboarding Data

### GET /me/pending-invitations
**Auth:** Authenticated
**Response 200:**
```json
[
  {
    "id": "uuid",
    "organizationName": "string",
    "role": "Admin | Member",
    "expiresAt": "2026-03-23T09:00:00Z"
  }
]
```

### GET /users/me (modified)
**Auth:** Authenticated
**Response 200 (additions):**
```json
{
  "id": "uuid",
  "email": "string",
  "name": "string",
  "systemRole": "User | SystemAdmin",
  "orgCount": 0,
  "pendingInvitationCount": 0
}
```

---

## Phase 6: Member Management

### GET /organizations/{orgId}/members
**Auth:** Org Member
**Response 200:**
```json
[
  {
    "userId": "uuid",
    "userName": "string",
    "userEmail": "string",
    "role": "Owner | Admin | Member",
    "joinedAt": "2026-03-16T09:00:00Z"
  }
]
```

### PUT /organizations/{orgId}/members/{userId}/role
**Auth:** Org Owner or Admin
**Request:**
```json
{
  "role": "Admin | Member"
}
```
**Notes:** Cannot assign Owner role via API. Cannot change own role. Cannot demote last Owner.
**Response 200:** OrgMemberDto
**Response 400:** ProblemDetails (last owner, self-change)
**Response 403:** ProblemDetails
**Response 404:** ProblemDetails

### DELETE /organizations/{orgId}/members/{userId}
**Auth:** Org Owner or Admin
**Notes:** Cannot remove self. Cannot remove last Owner.
**Response 204:** No content
**Response 400:** ProblemDetails (last owner, self-removal)
**Response 403:** ProblemDetails
**Response 404:** ProblemDetails

---

## JWT Claims (Modified)

```json
{
  "sub": "user-uuid",
  "email": "admin@example.com",
  "name": "Admin User",
  "system_role": "SystemAdmin",
  "jti": "unique-token-id"
}
```

New claim: `system_role` -- "User" or "SystemAdmin". Defaults to "User" if absent (backward compatible).
