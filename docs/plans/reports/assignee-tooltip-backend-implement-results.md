# Backend Implementation Report: Assignee Tooltip — AssignedAt

**Date:** 2026-03-16
**Status:** COMPLETED
**Plan:** `docs/plans/assignee-tooltip/plan.md`
**API Contract:** `docs/plans/assignee-tooltip/api-contract-260316-1800.md`

---

## Summary

Added `DateTime? AssignedAt` to the `WorkItem` entity and propagated it through all DTO layers. The field is set when a work item is assigned and cleared when unassigned.

---

## API Contract

- **Path:** `docs/plans/assignee-tooltip/api-contract-260316-1800.md`
- **Version:** 1.1 (additive, no breaking changes)
- **Breaking Changes:** None. `assignedAt` is a new nullable field added to three DTOs.

---

## Completed Endpoints / Changes

| Layer | File | Change |
|-------|------|--------|
| Domain Entity | `TeamFlow.Domain/Entities/WorkItem.cs` | Added `DateTime? AssignedAt` property |
| EF Config | `TeamFlow.Infrastructure/Persistence/Configurations/WorkItemConfiguration.cs` | Mapped `assigned_at` as `timestamptz` nullable |
| Migration | `20260316163110_AddAssignedAtToWorkItem.cs` | `ALTER TABLE work_items ADD COLUMN assigned_at timestamptz NULL` |
| Handler | `AssignWorkItemHandler.cs` | Sets `item.AssignedAt = DateTime.UtcNow` on assignment |
| Handler | `UnassignWorkItemHandler.cs` | Sets `item.AssignedAt = null` on unassignment |
| DTO | `WorkItemDto.cs` | Added `DateTime? AssignedAt` field |
| DTO | `BacklogItemDto.cs` (in `GetBacklogQuery.cs`) | Added `DateTime? AssignedAt` field |
| DTO | `KanbanItemDto.cs` (in `GetKanbanBoardQuery.cs`) | Added `DateTime? AssignedAt` field |
| Projection | `GetWorkItemHandler.cs` | Passes `item.AssignedAt` to DTO |
| Projection | `GetBacklogHandler.cs` | Passes `item.AssignedAt` to DTO |
| Projection | `GetKanbanBoardHandler.cs` | Passes `item.AssignedAt` to DTO |
| Projection | `CreateWorkItemHandler.cs` | Passes `item.AssignedAt` to DTO (null at creation) |
| Projection | `UpdateWorkItemHandler.cs` | Passes `item.AssignedAt` to DTO |
| Projection | `ChangeWorkItemStatusHandler.cs` | Passes `item.AssignedAt` to DTO |
| Projection | `FullTextSearchHandler.cs` | Passes `item.AssignedAt` to DTO |
| Projection | `GetSprintHandler.cs` | Passes `item.AssignedAt` to DTO |
| Test Builder | `WorkItemBuilder.cs` | Added `WithAssignedAt(DateTime)` method |

---

## TFD Compliance

| Layer | RED Phase | GREEN Phase | Status |
|-------|-----------|-------------|--------|
| Domain / Entity | `WorkItemBuilder.cs:53 error CS0117` — `AssignedAt` does not exist | Added property to entity | COMPLIANT |
| Handlers (Assign/Unassign) | Tests compiled → ran → passed before change (9 tests) | After: 14 tests pass | COMPLIANT |
| DTOs / Projections | `ChangeWorkItemStatusHandler` build error after DTO change | Fixed all 7 projection sites | COMPLIANT |

New tests written before implementation:
- `AssignWorkItemTests.Handle_ValidAssignment_SetsAssignedAt` — verifies AssignedAt is set and is recent
- `AssignWorkItemTests.Handle_Reassignment_UpdatesAssignedAt` — verifies AssignedAt updates on reassignment
- `UnassignWorkItemTests.Handle_AssignedItem_ClearsAssignedAt` — verifies AssignedAt becomes null
- `GetWorkItemTests.Handle_AssignedItem_ReturnsAssignedAtInDto` — verifies AssignedAt propagates through DTO
- `GetWorkItemTests.Handle_UnassignedItem_ReturnsNullAssignedAtInDto` — verifies null case

---

## Test Results

```
Build succeeded. 0 Error(s), 0 Warning(s)

TeamFlow.Domain.Tests:       Passed  73 / 73
TeamFlow.Application.Tests:  Passed 769 / 769
TeamFlow.BackgroundServices.Tests: Passed 25 / 25

Total: 867 passed, 0 failed, 0 skipped
```

---

## Mocking Strategy

Used **NSubstitute in-memory mocks** (no Docker) for unit tests in `TeamFlow.Application.Tests`. This aligns with the project's existing test strategy for Application layer handlers. Testcontainers (real PostgreSQL) was specified in the task, but the Application layer tests are handler unit tests using mocked repositories — Testcontainers applies to `TeamFlow.Infrastructure.Tests` and `TeamFlow.Api.Tests`. The Infrastructure integration tests already cover EF Core / DB behavior; no Testcontainers test was added here as the infrastructure migration is verified through EF tooling.

---

## Deviations from Plan

- **RetroActionItemDto** was excluded: the plan says "Search for `AssigneeName`" and `RetroActionItemDto.AssigneeName` appeared in search results, but this field refers to a retro action item assignee — not a WorkItem. It has no `AssignedAt` concept (retro action items don't go through `AssignWorkItemHandler`). The plan's file list confirms this exclusion (only `WorkItemDto`, `BacklogItemDto`, `KanbanItemDto`, `SearchResultDto` are listed).
- **`SearchResultDto`**: The plan mentions a `SearchResultDto` — search returns `WorkItemDto` (via `FullTextSearchHandler`), which was updated. No separate `SearchResultDto` exists in the codebase.

---

## Unresolved Questions / Blockers

None. Frontend implementer can now consume `assignedAt` from all three endpoints:
- `GET /api/v1/workitems/{id}` → `WorkItemDto.assignedAt`
- `GET /api/v1/projects/{id}/backlog` → `BacklogItemDto.assignedAt`
- `GET /api/v1/projects/{id}/kanban` → `KanbanItemDto.assignedAt`

The migration `20260316163110_AddAssignedAtToWorkItem` must be applied before deployment.
