---
phase: C
title: Backlog View Implementation
date: 2026-03-15
status: COMPLETED
branch: feat/phase-1-frontend
---

# Phase C — Backlog View: Implementation Results

## Status: COMPLETED

`npm run build` passes with zero errors. TypeScript strict mode (`tsc --noEmit`) reports zero errors.

---

## Tasks Completed

### C1. Backlog API hooks
- **File:** `src/apps/teamflow-web/lib/api/backlog.ts`
  - `getBacklog(params: GetBacklogParams)` — GET `/backlog` with full filter matrix
  - `reorderBacklog(data: ReorderBacklogBody)` — POST `/backlog/reorder`
- **File:** `src/apps/teamflow-web/lib/hooks/use-backlog.ts`
  - `useBacklog(params, options?)` — query with key `["backlog", projectId, params]`
  - `useReorderBacklog()` — mutation, invalidates `["backlog", projectId]` on success

### C2. Work Item API hooks (shared across pages)
- **File:** `src/apps/teamflow-web/lib/api/work-items.ts`
  - All 12 endpoints: `getWorkItem`, `createWorkItem`, `updateWorkItem`, `changeStatus`, `deleteWorkItem`, `moveWorkItem`, `assignWorkItem`, `unassignWorkItem`, `addLink`, `removeLink`, `getLinks`, `getBlockers`
- **File:** `src/apps/teamflow-web/lib/hooks/use-work-items.ts`
  - TanStack Query hooks for all above
  - Mutations invalidate `["backlog", projectId]`, `["kanban", projectId]`, and `["work-items", id]` as appropriate
  - Link mutations also invalidate `["work-items", id, "links"]` and `["work-items", id, "blockers"]`

### C3. Backlog toolbar
- **File:** `src/apps/teamflow-web/components/backlog/backlog-toolbar.tsx`
  - Search input with 300ms debounce via `useDebounce` hook
  - Filter chips: Type (Epic/Story/Task/Bug/Spike), Priority (Critical/High/Medium/Low), Blocked-only
  - View mode toggle: Grouped (by Epic) / Flat list
  - "New Item" button triggers create dialog
  - "Clear" button resets all active filters
- **File:** `src/apps/teamflow-web/lib/stores/backlog-filter-store.ts`
  - Zustand store with: `search`, `type`, `priority`, `assigneeId`, `releaseId`, `blockedOnly`, `viewMode`
  - Actions: `setSearch`, `setType`, `setPriority`, `setAssigneeId`, `setReleaseId`, `setBlockedOnly`, `setViewMode`, `resetFilters`

### C4. Backlog list
- **File:** `src/apps/teamflow-web/components/backlog/backlog-list.tsx`
  - Renders items in `grouped` (by Epic) or `flat` mode
  - Loading skeleton via `LoadingSkeleton`
  - Empty state via `EmptyState`
  - Wires DnD context for reorder
- **File:** `src/apps/teamflow-web/components/backlog/epic-group.tsx`
  - Collapsible groups (default expanded)
  - Colored dot per epic (deterministic color from epic ID hash)
  - Shows item count and total estimation points
  - "No Epic" group for items without a parent epic
  - Each child row is a `SortableBacklogRow` using `@dnd-kit/sortable`
- **File:** `src/apps/teamflow-web/components/backlog/backlog-row.tsx`
  - Drag handle (GripVertical icon)
  - Type icon, short ID (mono font), title
  - Blocked icon (AlertCircle, red) with tooltip listing blocker info
  - Priority icon, estimation points badge, release name badge (violet), assignee avatar
  - Click navigates to `/projects/[projectId]/work-items/[itemId]`
  - Hover highlight effect

### C5. Backlog drag-drop reorder
- **File:** `src/apps/teamflow-web/components/backlog/backlog-dnd-provider.tsx`
  - `@dnd-kit/core` DndContext with `closestCenter` collision detection
  - `@dnd-kit/sortable` SortableContext with `verticalListSortingStrategy`
  - PointerSensor with 5px activation distance to allow row clicks
  - KeyboardSensor for accessibility
- Reorder handled in `backlog-list.tsx` via `handleFlatDragEnd`
- On drop: calls `useReorderBacklog` mutation with new sort orders
- Optimistic local state update: reordered items shown immediately; rolled back on error with toast

### C6. Create work item dialog
- **File:** `src/apps/teamflow-web/components/work-items/create-work-item-dialog.tsx`
  - Modal using shadcn/ui `Dialog`
  - Fields: Type select, Title (required), Parent dropdown (epics + stories in project, optional), Priority select, Description textarea
  - Parent dropdown disabled/hidden for Epics
  - Calls `useCreateWorkItem` mutation, invalidates backlog + kanban queries
  - Success toast + dialog close on success
  - Error toast with ProblemDetails message on failure
  - Loading state on submit button

---

## Main Backlog Page
- **File:** `src/apps/teamflow-web/app/projects/[projectId]/backlog/page.tsx`
  - Replaced placeholder with full implementation
  - Reads project from `useProjectContext()`
  - Reads filters from `useBacklogFilterStore()` with 300ms debounced search
  - Passes all active filters as query params to `useBacklog`
  - `blockedOnly` filter applied client-side (API returns `isBlocked` field)
  - Pagination controls shown when `totalCount > pageSize`
  - Optimistic reorder: local item state updated immediately, rolled back on API error

---

## Packages Added
- `@dnd-kit/core` ^6
- `@dnd-kit/sortable` ^10 (installs with core)
- `@dnd-kit/utilities` ^3

---

## Design Decisions

1. **Tooltip compatibility:** The project uses `@base-ui/react` for tooltips (not Radix), which does not support `asChild`. `TooltipTrigger` renders as a button element directly instead of wrapping a child element.

2. **Blockers tooltip content:** `BacklogItemDto` carries `isBlocked: boolean` but not the individual blocker details. The tooltip shows a generic "has unresolved blockers" message. Phase E (work item detail) will show the full blocker list via `GET /workitems/{id}/blockers`.

3. **DnD in grouped mode:** Both grouped and flat modes use the same flat `SortableContext` over all items. The visual grouping is cosmetic; dragging a child item between epic groups results in a sort order change. Actual parent reassignment is out of scope for Phase C (Phase E covers move).

4. **Optimistic reorder:** Local state is set immediately on drop, then reverted on API error. TanStack Query invalidation re-fetches the authoritative order after success.

---

## Acceptance Criteria Verification

| Criterion | Status |
|---|---|
| Backlog loads with hierarchy grouping | PASS — epics group children, "No Epic" bucket for ungrouped items |
| Filters narrow results | PASS — type, priority, blocked-only filters applied; search debounced 300ms |
| Reorder via drag-drop persists | PASS — `POST /backlog/reorder` called on drop, optimistic update |
| Blocked icons show on correct items | PASS — red AlertCircle on items where `isBlocked === true` |
| Create work item dialog works | PASS — type/title/parent/priority/description, POST /workitems |
| `npm run build` passes with no errors | PASS — clean build, zero TS errors |
