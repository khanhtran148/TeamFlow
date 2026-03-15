# API Contract -- Sprint Endpoints (Integration Test Coverage)

**Version:** 1.0.0
**Date:** 2026-03-15T18:00
**Status:** FINAL -- used by Phase 2 backend integration tests

---

## Base URL

```
/api/v1/sprints
```

## Auth

All endpoints require `Authorization: Bearer <JWT>` except `/health`.

---

## Endpoints

### 1. POST /api/v1/sprints

Create a new sprint.

**Request Body:**
```json
{
  "projectId": "guid",
  "name": "string (required, max 100)",
  "goal": "string | null",
  "startDate": "yyyy-MM-dd | null",
  "endDate": "yyyy-MM-dd | null"
}
```

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 201 | Created | `SprintDto` |
| 400 | Validation error (missing name, invalid dates) | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | Insufficient permission (Developer, Viewer) | `ProblemDetails` |

**Required Permission:** `Sprint_Create`

---

### 2. GET /api/v1/sprints?projectId={guid}&page={int}&pageSize={int}

List sprints for a project with pagination.

**Query Parameters:**
- `projectId` (required): GUID
- `page` (optional, default 1): int
- `pageSize` (optional, default 20): int

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Success | `ListSprintsResult` |
| 401 | No auth token | - |
| 403 | No Project_View permission | `ProblemDetails` |

**Required Permission:** `Project_View` (all roles have this)

**Response Shape:**
```json
{
  "items": [SprintDto],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

---

### 3. GET /api/v1/sprints/{id}

Get sprint detail including items and capacity.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Success | `SprintDetailDto` |
| 401 | No auth token | - |
| 403 | No Project_View permission | `ProblemDetails` |
| 404 | Sprint not found | `ProblemDetails` |

**Required Permission:** `Project_View`

---

### 4. PUT /api/v1/sprints/{id}

Update sprint name, goal, dates. Only Planning sprints.

**Request Body:**
```json
{
  "name": "string (required)",
  "goal": "string | null",
  "startDate": "yyyy-MM-dd | null",
  "endDate": "yyyy-MM-dd | null"
}
```

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Updated | `SprintDto` |
| 400 | Validation error or not in Planning status | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Edit permission | `ProblemDetails` |

**Required Permission:** `Sprint_Edit`

---

### 5. DELETE /api/v1/sprints/{id}

Delete a sprint. Only Planning sprints.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 204 | Deleted | - |
| 400 | Not in Planning status | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Edit permission | `ProblemDetails` |

**Required Permission:** `Sprint_Edit`

---

### 6. POST /api/v1/sprints/{id}/start

Start a sprint. Requires Planning status, dates set, at least 1 item.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Started | `SprintDto` |
| 400 | Missing dates or items | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Start permission | `ProblemDetails` |
| 409 | Another sprint already active | `ProblemDetails` |

**Required Permission:** `Sprint_Start`

---

### 7. POST /api/v1/sprints/{id}/complete

Complete a sprint. Requires Active status.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Completed | `SprintDto` |
| 400 | Not in Active status | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Complete permission | `ProblemDetails` |

**Required Permission:** `Sprint_Complete`

---

### 8. POST /api/v1/sprints/{id}/items/{workItemId}

Add a work item to a sprint.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Added | - |
| 400 | Sprint is Completed or work item not found | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Edit permission | `ProblemDetails` |
| 409 | Work item already in another sprint | `ProblemDetails` |

**Required Permission:** `Sprint_Edit` (Planning) or `Sprint_Start` (Active)

---

### 9. DELETE /api/v1/sprints/{id}/items/{workItemId}

Remove a work item from a sprint.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 204 | Removed | - |
| 400 | Work item not in this sprint | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Edit permission | `ProblemDetails` |

**Required Permission:** `Sprint_Edit`

---

### 10. PUT /api/v1/sprints/{id}/capacity

Update capacity for sprint members. Only Planning sprints.

**Request Body:**
```json
{
  "capacity": [
    { "memberId": "guid", "points": 10 }
  ]
}
```

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Updated | - |
| 400 | Not in Planning status or invalid body | `ProblemDetails` |
| 401 | No auth token | - |
| 403 | No Sprint_Edit permission | `ProblemDetails` |

**Required Permission:** `Sprint_Edit`

---

### 11. GET /api/v1/sprints/{id}/burndown

Get burndown data for a sprint.

**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 200 | Success | `BurndownDto` |
| 401 | No auth token | - |
| 403 | No Project_View permission | `ProblemDetails` |
| 404 | Sprint not found | `ProblemDetails` |

**Required Permission:** `Project_View`

---

## Shared Types

### SprintDto
```json
{
  "id": "guid",
  "projectId": "guid",
  "name": "string",
  "goal": "string | null",
  "startDate": "yyyy-MM-dd | null",
  "endDate": "yyyy-MM-dd | null",
  "status": "Planning | Active | Completed",
  "totalPoints": 0,
  "completedPoints": 0,
  "itemCount": 0,
  "capacityUtilization": null,
  "createdAt": "ISO 8601"
}
```

### SprintDetailDto
Extends SprintDto with:
```json
{
  "items": [WorkItemDto],
  "capacity": [CapacityEntryDto]
}
```

### BurndownDto
```json
{
  "sprintId": "guid",
  "idealLine": [{ "date": "yyyy-MM-dd", "points": 0 }],
  "actualLine": [{ "date": "yyyy-MM-dd", "remainingPoints": 0, "completedPoints": 0, "addedPoints": 0 }]
}
```

### ListSprintsResult
```json
{
  "items": [SprintDto],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

---

## Permission Matrix

| Endpoint | OrgAdmin | ProductOwner | TechLead | TeamManager | Developer | Viewer |
|----------|----------|--------------|----------|-------------|-----------|--------|
| POST /sprints | 201 | 201 | 201 | 201 | 403 | 403 |
| GET /sprints | 200 | 200 | 200 | 200 | 200 | 200 |
| GET /sprints/{id} | 200 | 200 | 200 | 200 | 200 | 200 |
| PUT /sprints/{id} | 200 | 200 | 200 | 200 | 403 | 403 |
| DELETE /sprints/{id} | 204 | 204 | 204 | 204 | 403 | 403 |
| POST .../start | 200 | 403 | 403 | 200 | 403 | 403 |
| POST .../complete | 200 | 403 | 403 | 200 | 403 | 403 |
| POST .../items/{wid} | 200 | 200 | 200 | 200 | 403 | 403 |
| DELETE .../items/{wid} | 204 | 204 | 204 | 204 | 403 | 403 |
| PUT .../capacity | 200 | 200 | 200 | 200 | 403 | 403 |
| GET .../burndown | 200 | 200 | 200 | 200 | 200 | 200 |

Note: ProductOwner and TechLead do NOT have Sprint_Start/Sprint_Complete per PermissionMatrix.
OrgAdmin has all permissions. TeamManager has Sprint_Start and Sprint_Complete.

---

## Non-API Endpoints Tested

### GET /health
- Returns 200 with `{ status, checks[] }` JSON body
- No auth required

---

## TBD/Pending

None -- all endpoints are fully defined.
