# API Contract: Assignee Tooltip — AssignedAt Field

**Version:** 1.1 (minor, additive)
**Date:** 2026-03-16
**Base URL:** `/api/v1`
**Auth:** Bearer JWT (all endpoints require authentication)

---

## Summary of Changes

This contract documents the **additive** change to existing WorkItem-related endpoints.
`assignedAt` (`DateTime?`, ISO 8601 UTC) is added to four DTOs. No existing fields are removed or renamed. No new endpoints are introduced. Frontend callers may safely ignore the new field until they consume it.

---

## Modified DTOs

### WorkItemDto (breaking-field: none, additive)

**Affected endpoint:** `GET /api/v1/workitems/{id}`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "parentId": null,
  "type": "Task",
  "title": "Implement login page",
  "description": null,
  "status": "ToDo",
  "priority": "Medium",
  "estimationValue": 3.0,
  "assigneeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "assigneeName": "Jane Doe",
  "assignedAt": "2026-03-15T09:23:11Z",
  "sprintId": null,
  "releaseId": null,
  "childCount": 0,
  "linkCount": 0,
  "sortOrder": 0,
  "createdAt": "2026-03-10T08:00:00Z",
  "updatedAt": "2026-03-15T09:23:11Z"
}
```

**`assignedAt` rules:**
- `null` when no assignee is set
- Set to `DateTime.UtcNow` when `AssignWorkItemHandler` assigns an item
- Cleared to `null` when `UnassignWorkItemHandler` unassigns an item

---

### BacklogItemDto (breaking-field: none, additive)

**Affected endpoint:** `GET /api/v1/projects/{projectId}/backlog`

```json
{
  "id": "...",
  "projectId": "...",
  "parentId": null,
  "type": "UserStory",
  "title": "As a user I want to...",
  "status": "ToDo",
  "priority": "High",
  "assigneeId": "...",
  "assigneeName": "Jane Doe",
  "assignedAt": "2026-03-15T09:23:11Z",
  "releaseId": null,
  "releaseName": null,
  "isBlocked": false,
  "sortOrder": 1,
  "children": []
}
```

---

### KanbanItemDto (breaking-field: none, additive)

**Affected endpoint:** `GET /api/v1/projects/{projectId}/kanban`

```json
{
  "id": "...",
  "type": "Task",
  "title": "Build login form",
  "status": "InProgress",
  "priority": "High",
  "assigneeId": "...",
  "assigneeName": "Jane Doe",
  "assignedAt": "2026-03-15T09:23:11Z",
  "parentId": null,
  "parentTitle": null,
  "isBlocked": false,
  "releaseId": null
}
```

---

## Unchanged Endpoints

The following contain `AssigneeName` but are **out of scope** for this feature — the `RetroActionItem.AssigneeName` refers to a retro action item assignee, not a WorkItem:

- `GET /api/v1/retros/{id}` — `RetroActionItemDto.AssigneeName` unchanged (no `AssignedAt` needed)

---

## Error Responses

All errors remain unchanged — ProblemDetails (RFC 7807).

---

## TBD / Pending

- None. All scope items confirmed.

---

## Breaking Changes

None. This is a purely additive change. Existing API consumers can ignore the new `assignedAt` field.
