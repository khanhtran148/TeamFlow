---
version: "1.0"
phase: "2.3 — Team Management"
date: "2026-03-15"
author: backend-implementer
---

# API Contract — Phase 2.3: Team Management

## Base URL
`/api/v1`

## Auth
All endpoints require `Authorization: Bearer <jwt>` header. Returns `401 Unauthorized` when missing or expired.

---

## Teams

### POST /teams
Create a new team within an organization.

**Permission:** `Team_Manage` on the given `orgId`

**Request Body**
```json
{
  "orgId": "uuid",
  "name": "string (1–100 chars, required)",
  "description": "string (optional, max 2000)"
}
```

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 201 | `TeamDto` | Team created |
| 400 | `ProblemDetails` | Validation error |
| 403 | `ProblemDetails` | Access denied |

---

### GET /teams/{id}
Get a single team by ID.

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 200 | `TeamDto` | Team found |
| 404 | `ProblemDetails` | Team not found |

---

### GET /teams?orgId=&page=&pageSize=
List teams for an organization with pagination.

**Query Parameters**
- `orgId` (required) — `uuid`
- `page` (default: 1) — `int`
- `pageSize` (default: 20) — `int`

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 200 | `{ items: TeamDto[], totalCount: int, page: int, pageSize: int }` | Paginated list |
| 400 | `ProblemDetails` | Validation error |

---

### PUT /teams/{id}
Update team name and description.

**Permission:** `Team_Manage` on the team's `orgId`

**Request Body**
```json
{
  "name": "string (1–100 chars, required)",
  "description": "string (optional, max 2000)"
}
```

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 200 | `TeamDto` | Updated team |
| 400 | `ProblemDetails` | Validation error |
| 403 | `ProblemDetails` | Access denied |
| 404 | `ProblemDetails` | Team not found |

---

### DELETE /teams/{id}
Delete a team.

**Permission:** `Team_Manage` on the team's `orgId`

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 204 | (empty) | Deleted |
| 403 | `ProblemDetails` | Access denied |
| 404 | `ProblemDetails` | Team not found |

---

### POST /teams/{id}/members
Add a member to a team.

**Permission:** `Team_Manage` on the team's `orgId`

**Request Body**
```json
{
  "userId": "uuid",
  "role": "ProjectRole enum value"
}
```

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 204 | (empty) | Member added |
| 400 | `ProblemDetails` | Validation error |
| 403 | `ProblemDetails` | Access denied |
| 404 | `ProblemDetails` | Team not found |
| 409 | `ProblemDetails` | User already a member |

---

### DELETE /teams/{id}/members/{userId}
Remove a member from a team.

**Permission:** `Team_Manage` on the team's `orgId`

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 204 | (empty) | Member removed |
| 403 | `ProblemDetails` | Access denied |
| 404 | `ProblemDetails` | Team or member not found |

---

### PUT /teams/{id}/members/{userId}/role
Change the role of a team member.

**Permission:** `Team_Manage` on the team's `orgId`

**Request Body**
```json
{
  "newRole": "ProjectRole enum value"
}
```

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 200 | `TeamMemberDto` | Updated member |
| 403 | `ProblemDetails` | Access denied |
| 404 | `ProblemDetails` | Team or member not found |

---

## Project Memberships

### GET /projects/{projectId}/memberships
List all memberships for a project.

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 200 | `ProjectMembershipDto[]` | List of memberships |

---

### POST /projects/{projectId}/memberships
Add a user or team to a project.

**Permission:** `Project_ManageMembers` on the given `projectId`

**Request Body**
```json
{
  "memberId": "uuid",
  "memberType": "User | Team",
  "role": "ProjectRole enum value"
}
```

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 201 | `ProjectMembershipDto` | Membership created |
| 400 | `ProblemDetails` | Validation error |
| 403 | `ProblemDetails` | Access denied |
| 409 | `ProblemDetails` | Already a member |

---

### DELETE /projects/{projectId}/memberships/{membershipId}
Remove a membership from a project.

**Permission:** `Project_ManageMembers` on the membership's `projectId`

**Responses**
| Status | Body | Description |
|--------|------|-------------|
| 204 | (empty) | Removed |
| 403 | `ProblemDetails` | Access denied |
| 404 | `ProblemDetails` | Membership not found |

---

## Shared Types

### TeamDto
```json
{
  "id": "uuid",
  "orgId": "uuid",
  "name": "string",
  "description": "string | null",
  "memberCount": "int",
  "createdAt": "ISO 8601 UTC"
}
```

### TeamMemberDto
```json
{
  "id": "uuid",
  "userId": "uuid",
  "userName": "string",
  "userEmail": "string",
  "role": "ProjectRole",
  "joinedAt": "ISO 8601 UTC"
}
```

### ProjectMembershipDto
```json
{
  "id": "uuid",
  "projectId": "uuid",
  "memberId": "uuid",
  "memberType": "User | Team",
  "memberName": "string",
  "role": "ProjectRole",
  "createdAt": "ISO 8601 UTC"
}
```

---

## TBD / Pending

- `memberName` in `ProjectMembershipDto` — currently stubbed as "Unknown" for `Team` member type until team name lookup is added.
- `userName`/`userEmail` in `TeamMemberDto` — populated via `IUserRepository` lookup.
