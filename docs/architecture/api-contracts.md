---
type: api-contracts
description: REST API endpoint reference for TeamFlow v1 — routes, request/response types, status codes
---

# API Contracts

**Base URL:** `/api/v1/[controller]`
**Auth:** Phase 2 — not yet enforced (seed user context used)
**Error format:** `ProblemDetails` (RFC 7807) on all error responses
**Pagination response shape:** `{ items: [], totalCount: N, page: N, pageSize: N }`
**Dates:** ISO 8601 UTC — `2026-03-15T09:23:11Z`
**IDs:** UUID v4 as strings in JSON

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
| 201 Created | `ProjectDto` | Success; `Location` header set |
| 400 Bad Request | `ProblemDetails` | Validation failure |

---

### GET /api/v1/projects/{id}

Returns a single project.

**Path:** `id` — project UUID

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `ProjectDto` | Found |
| 404 Not Found | `ProblemDetails` | No project with that ID |

---

### GET /api/v1/projects

Returns a paginated list of projects.

**Query parameters**

| Parameter | Type | Default |
|---|---|---|
| orgId | uuid | — |
| status | string (`Active`, `Archived`) | — |
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

**Request body:** `UpdateProjectBody`

| Field | Type | Required |
|---|---|---|
| name | string (max 100) | yes |
| description | string | no |

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | `ProjectDto` | Updated |
| 400 Bad Request | `ProblemDetails` | Validation failure |
| 404 Not Found | `ProblemDetails` | Project not found |

---

### POST /api/v1/projects/{id}/archive

Archives a project (sets status to `Archived`).

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Archived |
| 404 Not Found | `ProblemDetails` | Project not found |

---

### DELETE /api/v1/projects/{id}

Deletes a project.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Deleted |
| 404 Not Found | `ProblemDetails` | Project not found |

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

**Request body:** `UpdateWorkItemBody`

| Field | Type | Required |
|---|---|---|
| title | string (max 500) | yes |
| description | string | no |
| priority | `Priority` | no |
| estimationValue | decimal | no |
| acceptanceCriteria | string | no |

`Priority` values: `Critical`, `High`, `Medium`, `Low`

**Responses**

| Status | Body |
|---|---|
| 200 OK | `WorkItemDto` |
| 400 Bad Request | `ProblemDetails` |

---

### POST /api/v1/workitems/{id}/status

Changes the status of a work item.

**Request body:** `ChangeStatusBody`

| Field | Type | Required |
|---|---|---|
| status | `WorkItemStatus` | yes |

`WorkItemStatus` values: `ToDo`, `InProgress`, `InReview`, `NeedsClarification`, `Done`, `Rejected`

**Responses**

| Status | Body |
|---|---|
| 200 OK | `WorkItemDto` |
| 400 Bad Request | `ProblemDetails` |

---

### DELETE /api/v1/workitems/{id}

Soft-deletes a work item. If the item has children, all descendants are soft-deleted.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Deleted |
| 404 Not Found | `ProblemDetails` | Not found |

---

### POST /api/v1/workitems/{id}/move

Moves a work item to a new parent (or to top level if `newParentId` is null).

**Request body:** `MoveWorkItemBody`

| Field | Type | Required |
|---|---|---|
| newParentId | uuid | no (null = top level) |

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |
| 400 Bad Request | `ProblemDetails` |

---

### POST /api/v1/workitems/{id}/assign

Assigns a work item to a user.

**Request body:** `AssignBody`

| Field | Type | Required |
|---|---|---|
| assigneeId | uuid | yes |

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |
| 400 Bad Request | `ProblemDetails` |

---

### POST /api/v1/workitems/{id}/unassign

Removes the assignee from a work item.

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |

---

### POST /api/v1/workitems/{id}/links

Adds a link between two work items. The reverse link is created automatically.

**Request body:** `AddLinkBody`

| Field | Type | Required |
|---|---|---|
| targetId | uuid | yes |
| linkType | `LinkType` | yes |

`LinkType` values: `Blocks`, `RelatesTo`, `Duplicates`, `DependsOn`, `Causes`, `Clones`

**Responses**

| Status | Body | Condition |
|---|---|---|
| 200 OK | — | Linked |
| 400 Bad Request | `ProblemDetails` | Circular dependency or validation failure |
| 409 Conflict | `ProblemDetails` | Link already exists |

---

### DELETE /api/v1/workitems/{id}/links/{linkId}

Removes a link by its ID. The reverse link is removed automatically.

**Responses**

| Status | Body | Condition |
|---|---|---|
| 204 No Content | — | Removed |
| 404 Not Found | `ProblemDetails` | Link not found |

---

### GET /api/v1/workitems/{id}/links

Returns all links for a work item, grouped by link type.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `WorkItemLinksDto` |

```json
{
  "workItemId": "uuid",
  "groups": [
    {
      "linkType": "Blocks",
      "items": [
        {
          "id": "uuid",
          "title": "string",
          "type": "Task",
          "status": "InProgress",
          "scope": "SameProject"
        }
      ]
    }
  ]
}
```

---

### GET /api/v1/workitems/{id}/blockers

Returns the list of unresolved blockers for a work item.

**Responses**

| Status | Body |
|---|---|
| 200 OK | `BlockersDto` |

```json
{
  "workItemId": "uuid",
  "hasUnresolvedBlockers": true,
  "blockers": [
    {
      "blockerId": "uuid",
      "title": "string",
      "status": "InProgress"
    }
  ]
}
```

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

**Request body:** `CreateReleaseCommand`

| Field | Type | Required |
|---|---|---|
| projectId | uuid | yes |
| name | string (max 100) | yes |
| description | string | no |
| releaseDate | date (ISO 8601) | no |

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

**Query parameters**

| Parameter | Type | Required | Default |
|---|---|---|---|
| projectId | uuid | yes | — |
| page | int | no | 1 |
| pageSize | int | no | 20 |

**Responses**

| Status | Body |
|---|---|
| 200 OK | Paginated list of `ReleaseDto` |

---

### PUT /api/v1/releases/{id}

Updates a release.

**Request body:** `UpdateReleaseBody`

| Field | Type | Required |
|---|---|---|
| name | string (max 100) | yes |
| description | string | no |
| releaseDate | date | no |

**Responses**

| Status | Body |
|---|---|
| 200 OK | `ReleaseDto` |

---

### DELETE /api/v1/releases/{id}

Deletes a release. Unlinks all associated work items first.

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

Updates sort order for one or more work items in bulk.

**Request body:** `ReorderBacklogCommand`

| Field | Type | Required |
|---|---|---|
| projectId | uuid | yes |
| items | `WorkItemSortOrder[]` | yes |

`WorkItemSortOrder`: `{ workItemId: uuid, sortOrder: int }`

**Responses**

| Status | Body |
|---|---|
| 200 OK | — |

---

## Kanban — `/api/v1/kanban`

### GET /api/v1/kanban

Returns the Kanban board for a project. Items are grouped into four status columns.

**Query parameters**

| Parameter | Type | Default |
|---|---|---|
| projectId | uuid (required) | — |
| assigneeId | uuid | — |
| type | `WorkItemType` | — |
| priority | `Priority` | — |
| sprintId | uuid | — |
| releaseId | uuid | — |
| swimlane | string (`assignee`, `epic`) | — |

**Responses**

| Status | Body |
|---|---|
| 200 OK | `KanbanBoardDto` |

```json
{
  "projectId": "uuid",
  "columns": [
    {
      "status": "ToDo",
      "itemCount": 5,
      "items": [
        {
          "id": "uuid",
          "type": "Task",
          "title": "string",
          "status": "ToDo",
          "priority": "High",
          "assigneeId": "uuid | null",
          "assigneeName": "string | null",
          "parentId": "uuid | null",
          "parentTitle": "string | null",
          "isBlocked": false,
          "releaseId": "uuid | null"
        }
      ]
    }
  ]
}
```

Columns are always returned in this order: `ToDo`, `InProgress`, `InReview`, `Done`.

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
| 403 Forbidden | Insufficient permission (Phase 2+) |
| 404 Not Found | Entity not found |
| 409 Conflict | Duplicate or conflicting state |
| 429 Too Many Requests | Rate limit exceeded (Phase 2+) |
| 500 Internal Server Error | Unhandled exception |
