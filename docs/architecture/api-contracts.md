---
type: api-contracts
description: REST API endpoint reference for TeamFlow v1 — routes, request/response types, status codes
---

# API Contracts

**Base URL:** `/api/v1/[controller]`
**Auth:** JWT Bearer token required on all endpoints except `/auth/register`, `/auth/login`, `/auth/refresh`
**Error format:** `ProblemDetails` (RFC 7807) on all error responses
**Pagination response shape:** `{ items: [], totalCount: N, page: N, pageSize: N }`
**Dates:** ISO 8601 UTC — `2026-03-15T09:23:11Z`
**IDs:** UUID v4 as strings in JSON

---

## Auth — `/api/v1/auth`

### POST /api/v1/auth/register

Registers a new user. Returns JWT + refresh token pair.

**Rate limit:** `Auth` policy

**Request body:** `RegisterCommand`

| Field | Type | Required |
|---|---|---|
| name | string | yes |
| email | string (email format) | yes |
| password | string (min 8 chars) | yes |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `AuthResponse` | Registered |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 409 Conflict | `ProblemDetails` | Email already in use |

---

### POST /api/v1/auth/login

Authenticates a user. Returns JWT + refresh token pair.

**Rate limit:** `Auth` policy

**Request body:** `LoginCommand`

| Field | Type | Required |
|---|---|---|
| email | string | yes |
| password | string | yes |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `AuthResponse` | Authenticated |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 401 Unauthorized | `ProblemDetails` | Invalid credentials |

---

### POST /api/v1/auth/refresh

Issues a new token pair using a valid refresh token.

**Rate limit:** `Auth` policy

**Request body:** `RefreshTokenCommand`

| Field | Type | Required |
|---|---|---|
| refreshToken | string | yes |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `AuthResponse` | Refreshed |
| 401 Unauthorized | `ProblemDetails` | Invalid or expired refresh token |

---

### POST /api/v1/auth/change-password

Changes the authenticated user's password.

**Rate limit:** `Write` policy

**Request body:** `ChangePasswordCommand`

| Field | Type | Required |
|---|---|---|
| currentPassword | string | yes |
| newPassword | string (min 8 chars) | yes |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Changed |
| 400 Bad Request | `ProblemDetails` | Current password incorrect or validation failure |

---

### POST /api/v1/auth/logout

Revokes the current user's refresh token.

**Rate limit:** `Write` policy

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |

---

### AuthResponse

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "base64string",
  "expiresIn": 1800
}
```

---

## Organizations — `/api/v1/organizations`

### POST /api/v1/organizations

Creates a new organization.

**Request body:** `CreateOrganizationCommand`

| Field | Type | Required |
|---|---|---|
| name | string | yes |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `OrganizationDto` | Created; `Location` header set |
| 400 Bad Request | `ProblemDetails` | Validation failure |

---

### GET /api/v1/organizations/{id}

Returns a single organization.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `OrganizationDto` | Found |
| 404 Not Found | — | Not found |

---

### GET /api/v1/organizations

Returns all organizations accessible to the current user.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `OrganizationDto[]` |

---

### OrganizationDto

```json
{
  "id": "uuid",
  "name": "string",
  "createdAt": "2026-03-15T09:23:11Z"
}
```

---

## Teams — `/api/v1/teams`

### POST /api/v1/teams

Creates a new team.

**Request body:** `CreateTeamCommand`

| Field | Type | Required |
|---|---|---|
| orgId | uuid | yes |
| name | string | yes |
| description | string | no |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `TeamDto` | Created; `Location` header set |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |

---

### GET /api/v1/teams/{id}

Returns a single team with its members.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `TeamDto` | Found |
| 404 Not Found | `ProblemDetails` | Not found |

---

### GET /api/v1/teams

Returns a paginated list of teams for an organization.

**Query parameters**

| Parameter | Type | Required | Default |
|---|---|---|---|
| orgId | uuid | yes | — |
| page | int | no | 1 |
| pageSize | int | no | 20 |

**Responses**

| Status | Body |
|---|---|
| 200 OK | `PagedResult<TeamDto>` |

---

### PUT /api/v1/teams/{id}

Updates team name and description.

**Request body:** `{ name: string, description: string? }`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `TeamDto` | Updated |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 404 Not Found | `ProblemDetails` | Not found |

---

### DELETE /api/v1/teams/{id}

Deletes a team.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Deleted |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 404 Not Found | `ProblemDetails` | Not found |

---

### POST /api/v1/teams/{id}/members

Adds a user to the team.

**Request body:** `{ userId: uuid, role: ProjectRole }`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Added |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 404 Not Found | `ProblemDetails` | User or team not found |
| 409 Conflict | `ProblemDetails` | User already a member |

---

### DELETE /api/v1/teams/{id}/members/{userId}

Removes a user from the team.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Removed |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 404 Not Found | `ProblemDetails` | Membership not found |

---

### PUT /api/v1/teams/{id}/members/{userId}/role

Changes a team member's role.

**Request body:** `{ newRole: ProjectRole }`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `TeamMemberDto` | Updated |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 404 Not Found | `ProblemDetails` | Membership not found |

---

### TeamDto / TeamMemberDto

```json
{
  "id": "uuid",
  "orgId": "uuid",
  "name": "string",
  "description": "string | null",
  "members": [
    {
      "userId": "uuid",
      "name": "string",
      "email": "string",
      "role": "Developer | TechnicalLeader | ProductOwner | ScrumMaster | Viewer | Admin"
    }
  ],
  "createdAt": "2026-03-15T09:23:11Z"
}
```

---

## Project Memberships — `/api/v1/projects/{projectId}/memberships`

### GET /api/v1/projects/{projectId}/memberships

Lists all memberships for a project.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `ProjectMembershipDto[]` |

---

### POST /api/v1/projects/{projectId}/memberships

Adds a user or team to the project.

**Request body:**

| Field | Type | Required |
|---|---|---|
| memberId | uuid | yes |
| memberType | `"User"` or `"Team"` | yes |
| role | `ProjectRole` | yes |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `ProjectMembershipDto` | Added |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 409 Conflict | `ProblemDetails` | Already a member |

---

### GET /api/v1/projects/{projectId}/memberships/me

Returns the current user's effective permissions for the project.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `MyPermissionsDto` |

---

### DELETE /api/v1/projects/{projectId}/memberships/{membershipId}

Removes a membership from the project.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Removed |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |
| 404 Not Found | `ProblemDetails` | Membership not found |

---

## Projects — `/api/v1/projects`

### POST /api/v1/projects

Creates a new project.

**Request body:** `CreateProjectCommand`

| Field | Type | Required |
|---|---|---|
| orgId | uuid | yes |
| name | string (max 100) | yes |
| description | string | no |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `ProjectDto` | Created; `Location` header set |
| 400 Bad Request | `ProblemDetails` | Validation failure |

---

### GET /api/v1/projects/{id}

Returns a single project.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `ProjectDto` | Found |
| 404 Not Found | `ProblemDetails` | Not found |

---

### GET /api/v1/projects

Returns a paginated list of projects.

**Query parameters**

| Parameter | Type | Default |
|---|---|---|
| orgId | uuid | — |
| status | `Active` or `Archived` | — |
| search | string | — |
| page | int | 1 |
| pageSize | int | 20 |

**Responses**

| Status | Body |
|---|---|
| 200 OK | Paginated list of `ProjectDto` |

---

### PUT /api/v1/projects/{id}

Updates project name and description.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `ProjectDto` | Updated |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 404 Not Found | `ProblemDetails` | Not found |

---

### POST /api/v1/projects/{id}/archive

Archives a project (sets status to `Archived`).

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Archived |
| 404 Not Found | `ProblemDetails` | Not found |

---

### DELETE /api/v1/projects/{id}

Deletes a project.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Deleted |
| 404 Not Found | `ProblemDetails` | Not found |

---

### ProjectDto

```json
{
  "id": "uuid",
  "orgId": "uuid",
  "name": "string",
  "description": "string | null",
  "status": "Active | Archived",
  "epicCount": 0,
  "openItemCount": 0,
  "createdAt": "2026-03-15T09:23:11Z",
  "updatedAt": "2026-03-15T09:23:11Z"
}
```

---

## Sprints — `/api/v1/sprints`

### POST /api/v1/sprints

Creates a new sprint.

**Rate limit:** `Write` policy

**Request body:** `CreateSprintCommand`

| Field | Type | Required |
|---|---|---|
| projectId | uuid | yes |
| name | string | yes |
| goal | string | no |
| startDate | date (ISO 8601) | no |
| endDate | date (ISO 8601) | no |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `SprintDto` | Created; `Location` header set |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 403 Forbidden | `ProblemDetails` | Insufficient permission |

---

### GET /api/v1/sprints/{id}

Returns a single sprint with its work items.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `SprintDetailDto` | Found |
| 404 Not Found | `ProblemDetails` | Not found |

---

### GET /api/v1/sprints

Returns a paginated list of sprints for a project.

**Query parameters**

| Parameter | Type | Default |
|---|---|---|
| projectId | uuid (required) | — |
| page | int | 1 |
| pageSize | int | 20 |

**Responses**

| Status | Body |
|---|---|
| 200 OK | `ListSprintsResult` |

---

### PUT /api/v1/sprints/{id}

Updates sprint name, goal, and dates.

**Rate limit:** `Write` policy

**Request body:** `{ name: string, goal: string?, startDate: date?, endDate: date? }`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `SprintDto` | Updated |
| 400 Bad Request | `ProblemDetails` | Validation failure |

---

### DELETE /api/v1/sprints/{id}

Deletes a sprint. Only allowed when status is `Planned`.

**Rate limit:** `Write` policy

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Deleted |
| 400 Bad Request | `ProblemDetails` | Sprint is active or completed |

---

### POST /api/v1/sprints/{id}/start

Starts a sprint. Sets status to `Active`, publishes `SprintStartedDomainEvent`.

**Rate limit:** `Write` policy

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `SprintDto` | Started |
| 400 Bad Request | `ProblemDetails` | Already started or no items |
| 409 Conflict | `ProblemDetails` | Another sprint is already active in this project |

---

### POST /api/v1/sprints/{id}/complete

Completes a sprint. Sets status to `Completed`, publishes `SprintCompletedDomainEvent`.

**Rate limit:** `Write` policy

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `SprintDto` | Completed |
| 400 Bad Request | `ProblemDetails` | Sprint not active |

---

### POST /api/v1/sprints/{id}/items/{workItemId}

Adds a work item to the sprint.

**Rate limit:** `Write` policy

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Added |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 409 Conflict | `ProblemDetails` | Item already in sprint |

---

### DELETE /api/v1/sprints/{id}/items/{workItemId}

Removes a work item from the sprint.

**Rate limit:** `Write` policy

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Removed |

---

### PUT /api/v1/sprints/{id}/capacity

Sets per-member capacity for the sprint.

**Rate limit:** `Write` policy

**Request body:** `{ capacity: [{ memberId: uuid, points: int }] }`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Updated |
| 400 Bad Request | `ProblemDetails` | Validation failure |

---

### GET /api/v1/sprints/{id}/burndown

Returns the burndown chart data for a sprint.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `BurndownDto` | Found |
| 404 Not Found | `ProblemDetails` | Sprint not found |

```json
{
  "sprintId": "uuid",
  "plannedPoints": 42,
  "completedPoints": 15,
  "dataPoints": [
    {
      "recordedDate": "2026-03-17",
      "remainingPoints": 42,
      "completedPoints": 0,
      "isWeekend": false
    }
  ]
}
```

---

### SprintDto / SprintDetailDto

```json
{
  "id": "uuid",
  "projectId": "uuid",
  "name": "string",
  "goal": "string | null",
  "status": "Planned | Active | Completed",
  "startDate": "2026-03-17",
  "endDate": "2026-03-28",
  "plannedPoints": 42,
  "completedPoints": 0,
  "itemCount": 18,
  "createdAt": "2026-03-15T09:23:11Z"
}
```

`SprintDetailDto` includes an additional `items: WorkItemDto[]` field.

---

## Work Items — `/api/v1/workitems`

### POST /api/v1/workitems

Creates a new work item.

**Request body:** `CreateWorkItemCommand`

| Field | Type | Required |
|---|---|---|
| projectId | uuid | yes |
| parentId | uuid | no (null for Epic) |
| type | `WorkItemType` | yes |
| title | string (max 500) | yes |
| description | string | no |
| priority | `Priority` | no |
| acceptanceCriteria | string | no |

`WorkItemType` values: `Epic`, `UserStory`, `Task`, `Bug`, `Spike`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 201 Created | `WorkItemDto` | Created; `Location` header set |
| 400 Bad Request | `ProblemDetails` | Validation failure |

---

### GET /api/v1/workitems/{id}

Returns a single work item with full details.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `WorkItemDto` | Found |
| 404 Not Found | `ProblemDetails` | Not found |

---

### PUT /api/v1/workitems/{id}

Updates title, description, priority, estimation, and acceptance criteria.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `WorkItemDto` |
| 400 Bad Request | `ProblemDetails` |

---

### POST /api/v1/workitems/{id}/status

Changes the status of a work item.

`WorkItemStatus` values: `ToDo`, `InProgress`, `InReview`, `NeedsClarification`, `Done`, `Rejected`

**Responses**

| Status | Body |
|---|---|
| 200 OK | `WorkItemDto` |
| 400 Bad Request | `ProblemDetails` |

---

### DELETE /api/v1/workitems/{id}

Soft-deletes a work item and all its descendants.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Deleted |
| 404 Not Found | `ProblemDetails` | Not found |

---

### POST /api/v1/workitems/{id}/move

Moves a work item to a new parent (or to top level if `newParentId` is null).

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |
| 400 Bad Request | `ProblemDetails` |

---

### POST /api/v1/workitems/{id}/assign

Assigns a work item to a user.

**Request body:** `{ assigneeId: uuid }`

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |
| 400 Bad Request | `ProblemDetails` |

---

### POST /api/v1/workitems/{id}/unassign

Removes the assignee.

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |

---

### POST /api/v1/workitems/{id}/links

Adds a link between two work items.

`LinkType` values: `Blocks`, `RelatesTo`, `Duplicates`, `DependsOn`, `Causes`, `Clones`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Linked |
| 400 Bad Request | `ProblemDetails` | Circular dependency or validation failure |
| 409 Conflict | `ProblemDetails` | Link already exists |

---

### DELETE /api/v1/workitems/{id}/links/{linkId}

Removes a link by ID.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Removed |
| 404 Not Found | `ProblemDetails` | Link not found |

---

### GET /api/v1/workitems/{id}/links

Returns all links grouped by type.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `WorkItemLinksDto` |

---

### GET /api/v1/workitems/{id}/history

Returns the paginated change history for a work item.

**Query parameters:** `page` (default 1), `pageSize` (default 20)

**Responses**

| Status | Body |
|---|---|
| 200 OK | Paginated list of `WorkItemHistoryDto` |

---

### GET /api/v1/workitems/{id}/blockers

Returns unresolved blockers.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `BlockersDto` |

---

### WorkItemDto

```json
{
  "id": "uuid",
  "projectId": "uuid",
  "parentId": "uuid | null",
  "type": "Epic | UserStory | Task | Bug | Spike",
  "title": "string",
  "description": "string | null",
  "status": "ToDo | InProgress | InReview | NeedsClarification | Done | Rejected",
  "priority": "Critical | High | Medium | Low | null",
  "estimationValue": 5.0,
  "assigneeId": "uuid | null",
  "assigneeName": "string | null",
  "sprintId": "uuid | null",
  "releaseId": "uuid | null",
  "childCount": 0,
  "linkCount": 0,
  "sortOrder": 0,
  "createdAt": "2026-03-15T09:23:11Z",
  "updatedAt": "2026-03-15T09:23:11Z"
}
```

---

## Releases — `/api/v1/releases`

### POST /api/v1/releases

Creates a new release.

**Responses**

| Status | Body |
|---|---|
| 201 Created | `ReleaseDto` |

---

### GET /api/v1/releases/{id}

Returns a single release with item counts by status.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `ReleaseDto` | Found |
| 404 Not Found | `ProblemDetails` | Not found |

---

### GET /api/v1/releases

Returns a paginated list of releases for a project.

**Query parameters:** `projectId` (required), `page`, `pageSize`

**Responses**

| Status | Body |
|---|---|
| 200 OK | Paginated list of `ReleaseDto` |

---

### PUT /api/v1/releases/{id}

Updates a release.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `ReleaseDto` |

---

### DELETE /api/v1/releases/{id}

Deletes a release. Unlinks all work items.

**Responses**

| Status | Body |
|---|---|
| 204 No Content | — |

---

### POST /api/v1/releases/{id}/items/{workItemId}

Assigns a work item to this release.

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |
| 404 Not Found | `ProblemDetails` |

---

### DELETE /api/v1/releases/{id}/items/{workItemId}

Removes a work item from this release.

**Responses**

| Status | Body |
|---|---|
| 204 No Content | — |

---

### ReleaseDto

```json
{
  "id": "uuid",
  "projectId": "uuid",
  "name": "string",
  "description": "string | null",
  "releaseDate": "2026-04-01",
  "status": "Unreleased | Overdue | Released",
  "notesLocked": false,
  "totalItems": 12,
  "itemCountsByStatus": {
    "ToDo": 3,
    "InProgress": 5,
    "Done": 4
  },
  "createdAt": "2026-03-15T09:23:11Z"
}
```

---

## Backlog — `/api/v1/backlog`

### GET /api/v1/backlog

Returns the paginated, filtered backlog for a project.

**Query parameters**

| Parameter | Type | Default |
|---|---|---|
| projectId | uuid (required) | — |
| status | `WorkItemStatus` | — |
| priority | `Priority` | — |
| assigneeId | uuid | — |
| type | `WorkItemType` | — |
| sprintId | uuid | — |
| releaseId | uuid | — |
| unscheduled | bool | — |
| search | string | — |
| page | int | 1 |
| pageSize | int | 20 |

**Responses**

| Status | Body |
|---|---|
| 200 OK | Paginated list of `WorkItemDto` |

---

### POST /api/v1/backlog/reorder

Updates sort order for one or more work items.

**Request body:** `{ projectId: uuid, items: [{ workItemId: uuid, sortOrder: int }] }`

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |

---

## Kanban — `/api/v1/kanban`

### GET /api/v1/kanban

Returns the Kanban board for a project. Items are always grouped into four columns: `ToDo`, `InProgress`, `InReview`, `Done`.

**Query parameters**

| Parameter | Type | Default |
|---|---|---|
| projectId | uuid (required) | — |
| assigneeId | uuid | — |
| type | `WorkItemType` | — |
| priority | `Priority` | — |
| sprintId | uuid | — |
| releaseId | uuid | — |
| swimlane | `assignee` or `epic` | — |

**Responses**

| Status | Body |
|---|---|
| 200 OK | `KanbanBoardDto` |

---

## Common Error Responses

All error responses follow RFC 7807 `ProblemDetails`:

```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "Title must not be empty",
  "instance": "/api/v1/workitems",
  "correlationId": "uuid"
}
```

| Status | Trigger |
|---|---|
| 400 Bad Request | FluentValidation failure or business rule violation |
| 401 Unauthorized | Missing or invalid JWT |
| 403 Forbidden | Insufficient permission |
| 404 Not Found | Entity not found |
| 409 Conflict | Duplicate or conflicting state |
| 429 Too Many Requests | Rate limit exceeded |
| 500 Internal Server Error | Unhandled exception |
