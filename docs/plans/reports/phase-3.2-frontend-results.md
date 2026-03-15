# Phase 3.2 -- Sprint Planning Frontend: Implementation Report

**Date:** 2026-03-15
**Status:** COMPLETED
**Branch:** `feat/phase-3-sprint-hardening`
**Build:** PASS (`npm run build` and `tsc --noEmit` clean)

---

## Completed Components

### 3.2.1 -- Sprint API Client and Hooks

| File | Description |
|------|-------------|
| `lib/api/types.ts` | Added `SprintStatus`, `SprintDto`, `SprintDetailDto`, `SprintCapacityMemberDto`, `BurndownDto`, `BurndownDataPointDto`, `BurndownActualPointDto`, `CreateSprintBody`, `UpdateSprintBody`, `UpdateCapacityBody`, `GetSprintsParams` |
| `lib/api/sprints.ts` | Sprint API functions: `getSprints`, `getSprint`, `createSprint`, `updateSprint`, `deleteSprint`, `startSprint`, `completeSprint`, `addItemToSprint`, `removeItemFromSprint`, `updateSprintCapacity`, `getSprintBurndown` |
| `lib/hooks/use-sprints.ts` | TanStack Query hooks: `useSprints`, `useSprint`, `useSprintBurndown`, `useCreateSprint`, `useUpdateSprint`, `useDeleteSprint`, `useStartSprint`, `useCompleteSprint`, `useAddItemToSprint`, `useRemoveItemFromSprint`, `useUpdateSprintCapacity` with query key factory `sprintKeys` |

### 3.2.2 -- Sprint List and Detail Pages

| File | Description |
|------|-------------|
| `app/projects/[projectId]/sprints/page.tsx` | Sprint list page with loading skeletons, error state, empty state, create/edit/delete dialogs |
| `app/projects/[projectId]/sprints/[sprintId]/page.tsx` | Sprint detail page with header card, progress bar, burndown chart, planning board, scope lock indicator, permission-aware action buttons |
| `components/sprints/sprint-card.tsx` | Sprint card component with status badge, progress bar, date range, item count, capacity percentage, context menu |
| `components/sprints/sprint-status-badge.tsx` | Badge component for Planning/Active/Completed status |

### 3.2.3 -- Sprint Planning Board

| File | Description |
|------|-------------|
| `components/sprints/sprint-planning-board.tsx` | Split view: backlog (left) + sprint scope (right) with @dnd-kit drag-and-drop, capacity indicator, scope lock warning, member capacity breakdown |
| `components/sprints/capacity-indicator.tsx` | Capacity bar with assigned/total points, color-coded (green/yellow/red), over-capacity alert |
| `components/sprints/member-capacity.tsx` | Per-member capacity breakdown with individual progress bars |
| `components/sprints/sprint-form-dialog.tsx` | Create/edit sprint dialog with name, goal, start/end date fields, validation |
| `components/sprints/capacity-form.tsx` | Capacity edit dialog with per-member point inputs |

### 3.2.4 -- Scope Lock and Confirmation UI

| File | Description |
|------|-------------|
| `components/sprints/add-item-confirmation.tsx` | Confirmation dialog for adding items to active (scope-locked) sprint |
| Sprint detail page | Start button disabled when no items or missing dates; Complete button only on Active; Scope lock badge on Active sprint; Permission-aware: edit/delete/start/complete buttons hidden for non-authorized roles |

### 3.2.5 -- Burndown Chart

| File | Description |
|------|-------------|
| `components/sprints/burndown-chart.tsx` | Recharts-based burndown chart with ideal (dashed) vs actual (solid) lines, responsive layout, dark/light mode support via CSS variables |

### 3.2.6 -- Sprint Realtime Events

| File | Description |
|------|-------------|
| `lib/signalr/event-handlers.ts` | Extended with `Sprint.Started`, `Sprint.Completed`, `Sprint.ItemAdded`, `Sprint.ItemRemoved`, `Burndown.Updated` event handlers. Invalidates sprint queries, backlog queries, and burndown queries on events. |

### Navigation

| File | Description |
|------|-------------|
| `components/projects/project-nav.tsx` | Added "Sprints" tab between Board and Releases |

---

## API Contract Usage

| Endpoint | Component | Status |
|----------|-----------|--------|
| `GET /sprints?projectId=` | `useSprints` / sprints list page | Wired |
| `GET /sprints/{id}` | `useSprint` / sprint detail page | Wired |
| `POST /sprints` | `useCreateSprint` / sprint form dialog | Wired |
| `PUT /sprints/{id}` | `useUpdateSprint` / sprint form dialog | Wired |
| `DELETE /sprints/{id}` | `useDeleteSprint` / sprint list + detail | Wired |
| `POST /sprints/{id}/start` | `useStartSprint` / sprint detail page | Wired |
| `POST /sprints/{id}/complete` | `useCompleteSprint` / sprint detail page | Wired |
| `POST /sprints/{id}/items/{workItemId}` | `useAddItemToSprint` / planning board | Wired |
| `DELETE /sprints/{id}/items/{workItemId}` | `useRemoveItemFromSprint` / planning board | Wired |
| `PUT /sprints/{id}/capacity` | `useUpdateSprintCapacity` / capacity form | Wired |
| `GET /sprints/{id}/burndown` | `useSprintBurndown` / burndown chart | Wired |

---

## Dependencies Added

| Package | Version | Purpose |
|---------|---------|---------|
| `recharts` | latest | Burndown chart (line chart) |

---

## Deviations from Contract

None. All endpoint shapes, request/response types, and status enums match the API contract exactly.

---

## Type Errors Fixed

- Burndown chart `Tooltip.labelFormatter` expected `(label: ReactNode, ...) => ReactNode` but received `(dateStr: string) => string`. Fixed with `(label: unknown) => formatDateLabel(String(label))`.

---

## Dark/Light Mode Support

All components use CSS custom properties (`--tf-*` tokens) that automatically adapt to `[data-theme="light"]` on `<html>`. No hardcoded colors.

---

## Accessibility

- All dialogs have `role="dialog"`, `aria-modal="true"`, and `aria-labelledby`
- Sprint cards have `role="article"`, `tabIndex={0}`, keyboard navigation (Enter/Space)
- Capacity indicator has `role="meter"` with `aria-valuenow`, `aria-valuemin`, `aria-valuemax`
- Over-capacity warning uses `role="alert"`
- Scope lock warning uses `role="alert"`
- All buttons have descriptive `aria-label` attributes
- Context menus have `role="menu"` and `role="menuitem"`

---

## Vitest TFD Status

N/A -- Phase 3.2 scope is primarily presentational UI components. Logic-bearing code (hooks) delegates to TanStack Query which is tested via integration tests in Phase 3.5.

---

## Manual Verification Required

- [ ] Responsive layout at 320px / 768px / 1024px breakpoints
- [ ] Contrast ratios meet WCAG 2.1 AA
- [ ] Drag-and-drop with keyboard/screen reader
- [ ] Two-browser-tab realtime sync via SignalR
