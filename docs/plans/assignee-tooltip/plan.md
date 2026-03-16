# Plan: Assignee Tooltip with Name + Assignment Date

**Created:** 2026-03-16
**Scope:** Fullstack (small change: backend DTO + frontend tooltip)
**Status:** Awaiting approval

---

## Problem

When hovering over an assignee avatar icon, the browser shows a basic `title` tooltip with just the name. Users want to see the full name **and when the work item was assigned** to that person.

## Current State

- `UserAvatar` component already renders a native `title={name}` tooltip
- Backend DTOs (`WorkItemDto`, `BacklogItemDto`, `KanbanItemDto`) include `assigneeName` but **no `assignedAt`**
- The `WorkItem` entity has no `AssignedAt` column
- Assignment dates can be derived from `work_item_histories` (field_name = "AssigneeId")

## Approach

### Option A: Add `AssignedAt` column to WorkItem (Recommended)
- Add nullable `DateTime? AssignedAt` to `WorkItem` entity
- Set it in `AssignWorkItemHandler` / `UpdateWorkItemHandler` when assignee changes
- Include in DTOs, expose to frontend
- **Pro:** Single query, no joins, fast
- **Con:** Requires migration + handler changes

### Option B: Derive from work_item_histories at query time
- Join `work_item_histories` in each read query to find latest assignee change
- **Pro:** No schema change
- **Con:** Complex joins on every backlog/kanban query, performance concern with 1000+ items

**Decision: Option A** — add `AssignedAt` to the entity. Simpler, faster, aligns with the data model.

---

## Phase 1: Backend — Add AssignedAt

### Tasks
1. **Add `AssignedAt` to WorkItem entity** — nullable `DateTime?`
2. **Migration** — `AddAssignedAtToWorkItem`
3. **Update AssignWorkItemHandler** — set `AssignedAt = DateTime.UtcNow` when assigning
4. **Update UnassignWorkItemHandler** — set `AssignedAt = null` when unassigning
5. **Update DTOs** — add `assignedAt` to `WorkItemDto`, `BacklogItemDto`, `KanbanItemDto`, `SearchResultDto`
6. **Tests** — verify assignedAt is set/cleared correctly

### Files
- `src/core/TeamFlow.Domain/Entities/WorkItem.cs` (modify)
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/WorkItemConfiguration.cs` (modify)
- `src/core/TeamFlow.Infrastructure/Migrations/*_AddAssignedAtToWorkItem.cs` (new)
- `src/core/TeamFlow.Application/Features/WorkItems/AssignWorkItem/AssignWorkItemHandler.cs` (modify)
- `src/core/TeamFlow.Application/Features/WorkItems/UnassignWorkItem/UnassignWorkItemHandler.cs` (modify)
- DTOs: `WorkItemDto.cs`, backlog/kanban DTOs that include assignee info (modify)

## Phase 2: Frontend — Enhanced Tooltip

### Tasks
1. **Add `assignedAt` to TypeScript types** — `WorkItemDto`, `BacklogItemDto`, `KanbanItemDto`
2. **Update `UserAvatar` component** — accept optional `subtitle` prop for tooltip second line
3. **Update all assignee avatar usages** — pass `assignedAt` formatted as "Assigned Mar 15, 2026"
4. **Locations to update:**
   - `components/backlog/backlog-row.tsx`
   - `components/kanban/kanban-card.tsx`
   - `components/work-items/children-tab.tsx`
   - `components/work-items/assignee-picker.tsx`
   - `components/search/search-results.tsx`

### Tooltip Format
```
Jane Doe
Assigned Mar 15, 2026
```

Using native `title` attribute (multi-line with \n) — simple, no tooltip library needed.

---

## Acceptance Criteria
- [ ] Hovering over any assignee avatar shows full name + assignment date
- [ ] Unassigned items show no tooltip
- [ ] AssignedAt is set when assigning and cleared when unassigning
- [ ] Migration applies cleanly
- [ ] Backend tests verify assignedAt behavior
