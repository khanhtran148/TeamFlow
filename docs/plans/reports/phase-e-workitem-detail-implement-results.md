---
phase: E
title: Work Item Detail
status: COMPLETED
date: 2026-03-15
build: PASS
---

# Phase E — Work Item Detail: Implementation Results

## Status: COMPLETED

`npm run build` passes with zero TypeScript errors. All 6 tasks (E1–E6) are fully implemented.

---

## Tasks Completed

### E1. Work Item Detail Page
**File:** `app/projects/[projectId]/work-items/[workItemId]/page.tsx`

- Two-column layout: main content (left, ~65%) + sticky sidebar (right, 320px)
- Back-to-backlog navigation link + Delete button in top nav bar
- Fetches work item via `useWorkItem` hook; shows `LoadingSkeleton` while loading, `EmptyState` on error/404
- Title editable inline via `WorkItemHeader`
- Main form via `WorkItemForm` (description, acceptance criteria, estimation, priority)
- Tabs (Links | Children) below the form with counts from work item metadata
- Delete dialog integration with redirect to backlog on success

### E2. Status Change
**File:** `components/work-items/status-select.tsx`

- Dropdown showing all 6 statuses with `StatusBadge` visual for each
- Calls `POST /workitems/{id}/status` via `useChangeStatus` mutation
- When target is `InProgress`: shows confirm dialog, lazily fetches blockers (`useWorkItemBlockers` enabled only when dialog opens)
- If item has unresolved blockers: dialog lists each blocker title with red dot indicators
- "Move Anyway" confirms, Cancel aborts; toast on success/error

### E3. Assign / Unassign
**File:** `components/work-items/assignee-picker.tsx`

- Dropdown showing 5 seed users (Phase 1 — no user API)
- Selected user shown with `UserAvatar` + name; unassigned shows dashed placeholder
- Assign: `POST /workitems/{id}/assign` via `useAssignWorkItem`
- Unassign: `POST /workitems/{id}/unassign` via `useUnassignWorkItem` (X button on trigger or top of dropdown)
- Toast on success/error; loading spinner during pending

### E4. Links Tab
**Files:** `components/work-items/links-tab.tsx`, `components/work-items/add-link-dialog.tsx`

- Links grouped by `LinkType` with colored dot + label + count header per group
- Each link row: TypeIcon, short ID, title, StatusBadge, remove (X) button
- Remove calls `DELETE /workitems/{id}/links/{linkId}` via `useRemoveLink`
- Empty state with Link2 icon when no links
- "Add Link" opens `AddLinkDialog`:
  - Link type select (all 6 types with descriptions)
  - Live search using `useBacklog` hook (filters out current item)
  - Selected item preview showing relationship sentence
  - API error displayed inline with `AlertCircle` (catches circular dependency `ProblemDetails`)
  - Calls `POST /workitems/{id}/links` via `useAddLink`

### E5. Children Tab
**File:** `components/work-items/children-tab.tsx`

- Fetches backlog for projectId (100 items), filters client-side by `parentId === workItemId`
- Child row: TypeIcon, short ID, title, StatusBadge, UserAvatar
- Click navigates to child detail page via `router.push`
- "Add Child" opens existing `CreateWorkItemDialog` with `defaultParentId` pre-set
- Empty state with GitBranch icon when no children
- Loading skeleton while backlog fetches

### E6. Delete Work Item
**File:** `components/work-items/delete-work-item-dialog.tsx`

- Red warning dialog with `AlertTriangle` icon
- Shows cascade warning with child count if `workItem.childCount > 0`
- Notes soft-delete behaviour (item recoverable by admin)
- Calls `DELETE /workitems/{id}` via `useDeleteWorkItem`
- On success: toast + `onDeleted()` callback → page redirects to backlog

---

## Supporting Components

| File | Purpose |
|---|---|
| `components/work-items/work-item-header.tsx` | Inline-editable title with type icon and status badge |
| `components/work-items/work-item-form.tsx` | Description, acceptance criteria (stories/epics), estimation, priority with dirty-state Save button |
| `components/work-items/work-item-sidebar.tsx` | StatusSelect, AssigneePicker, release badge, parent link, created/updated dates |

---

## Bug Fix (Pre-existing)

`app/projects/[projectId]/releases/page.tsx` line 156 passed a plain object `{ label, onClick }` to `EmptyState.action` which expects `ReactNode`. Fixed by replacing with a proper `<button>` element.

---

## Acceptance Criteria Check

| Criterion | Status |
|---|---|
| View/edit all work item fields | PASS |
| Change status works | PASS |
| Blocked confirm dialog with blockers listed | PASS |
| Assign/unassign works | PASS |
| Links tab: view grouped links | PASS |
| Links tab: add link | PASS |
| Links tab: remove link | PASS |
| Circular dependency error shown from API ProblemDetails | PASS |
| Children tab: list children | PASS |
| Children tab: navigate to child | PASS |
| Children tab: add child with parent pre-set | PASS |
| Delete with cascade warning | PASS |
| `npm run build` passes with no errors | PASS |

---

## Files Created

- `app/projects/[projectId]/work-items/[workItemId]/page.tsx`
- `components/work-items/work-item-header.tsx`
- `components/work-items/work-item-form.tsx`
- `components/work-items/work-item-sidebar.tsx`
- `components/work-items/status-select.tsx`
- `components/work-items/assignee-picker.tsx`
- `components/work-items/links-tab.tsx`
- `components/work-items/add-link-dialog.tsx`
- `components/work-items/children-tab.tsx`
- `components/work-items/delete-work-item-dialog.tsx`

## Files Modified

- `app/projects/[projectId]/releases/page.tsx` — fixed pre-existing `EmptyState.action` type error
