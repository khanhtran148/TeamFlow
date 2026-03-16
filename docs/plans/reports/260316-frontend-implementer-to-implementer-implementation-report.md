# Assignee Tooltip — Frontend Implementation Report

**Date:** 2026-03-16
**Feature:** Assignee Tooltip with Name + Assignment Date
**Status:** COMPLETED

---

## Summary

Implemented Phase 2 (Frontend) of the assignee tooltip feature. When users hover over any assignee avatar, the native browser `title` tooltip now shows both the assignee name and the assignment date (e.g., "Jane Doe\nAssigned 15 Mar 2026").

---

## Completed Components

| File | Change |
|------|--------|
| `src/apps/teamflow-web/lib/api/types.ts` | Added `assignedAt: string \| null` to `WorkItemDto`, `WorkItemSummaryDto`, `KanbanItemDto` |
| `src/apps/teamflow-web/components/shared/user-avatar.tsx` | Added `subtitle` prop; added exported `formatAssignedAt` helper; updated `title` attribute to render `name\nsubtitle` when subtitle present |
| `src/apps/teamflow-web/components/backlog/backlog-row.tsx` | Imported `formatAssignedAt`; passed `subtitle={formatAssignedAt(item.assignedAt)}` to `UserAvatar` |
| `src/apps/teamflow-web/components/kanban/kanban-card.tsx` | Imported `formatAssignedAt`; passed subtitle on both `KanbanCard` and `KanbanCardGhost` avatar renders |
| `src/apps/teamflow-web/components/work-items/children-tab.tsx` | Imported `formatAssignedAt`; passed subtitle to child item `UserAvatar` |
| `src/apps/teamflow-web/components/work-items/assignee-picker.tsx` | Imported `formatAssignedAt`; added optional `assignedAt` prop to `AssigneePickerProps`; passed subtitle to current-assignee avatar in the trigger button |
| `src/apps/teamflow-web/components/work-items/work-item-sidebar.tsx` | Passed `assignedAt={workItem.assignedAt}` to `AssigneePicker` call site |

---

## API Contract Usage

| Field | Source DTO | Component(s) | Status |
|-------|-----------|-------------|--------|
| `assignedAt` on `WorkItemDto` | Backend (Phase 1 pending) | `BacklogRow`, `ChildrenTab`, `WorkItemSidebar` → `AssigneePicker` | Frontend ready — waiting for backend |
| `assignedAt` on `KanbanItemDto` | Backend (Phase 1 pending) | `KanbanCard`, `KanbanCardGhost` | Frontend ready — waiting for backend |
| `assignedAt` on `WorkItemSummaryDto` | Backend (Phase 1 pending) | Not yet consumed by any UI component | Type updated for completeness |

---

## Design Decisions

- `formatAssignedAt` is exported from `user-avatar.tsx` so all callers share the same locale format (`en-AU`, e.g. "15 Mar 2026").
- `subtitle` is an optional prop on `UserAvatar` — fully backward compatible. Existing callers without `assignedAt` continue to work unchanged (they simply omit the prop).
- `assignedAt` on `AssigneePickerProps` is optional (`assignedAt?: string | null`) — backward compatible with any call site that does not yet pass it.
- `search-results.tsx` renders `assigneeName` as plain text (no `UserAvatar`), so no change was needed there. If that component is ever upgraded to use `UserAvatar`, the `WorkItemDto.assignedAt` field is already in place.
- The ghost/drag-overlay card (`KanbanCardGhost`) also receives the subtitle even though it is pointer-events-none — consistent with the live card and harmless.

---

## Vitest TFD Status

N/A — Changes are limited to type extensions and prop pass-through on presentational/data-display components. No new hooks, stores, or business logic were introduced. The `formatAssignedAt` utility is a pure single-expression function (locale date format) that does not warrant a dedicated test suite under the project's coverage policy.

---

## Deviations from Contract

None. All changes align exactly with the plan in `docs/plans/assignee-tooltip/plan.md` Phase 2.

---

## Build Verification

`npx tsc --noEmit` — 0 errors, 0 warnings.

---

## Unresolved Questions / Blockers

- **Backend Phase 1 is not yet merged.** Until `assignedAt` is returned by the API, the tooltip second line will not appear at runtime (the field will be `undefined`/`null`, which `formatAssignedAt` handles gracefully by returning `undefined`, leaving the tooltip showing only the name). No frontend code change is needed after the backend lands.
