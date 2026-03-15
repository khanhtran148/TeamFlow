---
phase: F — Releases
status: COMPLETED
date: 2026-03-15
build: PASS
---

# Phase F — Releases: Implementation Results

## Status: COMPLETED

`npm run build` passes with zero errors. All 8 required routes render successfully (including the new `ƒ /projects/[projectId]/releases` and `ƒ /projects/[projectId]/releases/[releaseId]`).

---

## Files Created

### F1 — Release API & Hooks

| File | Purpose |
|---|---|
| `lib/api/releases.ts` | All release API functions: `getReleases`, `getRelease`, `createRelease`, `updateRelease`, `deleteRelease`, `assignItem`, `unassignItem` |
| `lib/hooks/use-releases.ts` | TanStack Query hooks: `useReleases`, `useRelease`, `useCreateRelease`, `useUpdateRelease`, `useDeleteRelease`, `useAssignItem`, `useUnassignItem` |

Query key conventions followed:
- `["releases", projectId]` — all releases for project (invalidated on any mutation)
- `["releases", projectId, "list", params]` — paginated list
- `["releases", "detail", id]` — single release

Mutations also invalidate `["backlog", projectId]` so release badges on backlog rows stay current.

### F2 — Release List Page

| File | Purpose |
|---|---|
| `app/projects/[projectId]/releases/page.tsx` | Full release list page (replaces placeholder) |
| `components/releases/release-card.tsx` | Release card with name, description, status badge, progress bar, item counts, edit/delete menu |
| `components/releases/create-release-dialog.tsx` | Create release form (name required, description + date optional) |
| `components/releases/edit-release-dialog.tsx` | Edit release form (pre-populated from existing release) |

Features:
- Responsive grid layout (`auto-fill, minmax(320px, 1fr)`)
- Status badges: Unreleased=green/accent, Overdue=red, Released=gray/muted
- Progress bar (done/total items, color matches status)
- Status breakdown chip row (e.g. "To Do: 3 · In Progress: 2")
- Loading skeleton grid (4 card skeletons)
- Empty state with "New Release" action button
- Confirm dialog for delete (using existing `ConfirmDialog` from `components/projects/`)

### F3 — Release Detail Page

| File | Purpose |
|---|---|
| `app/projects/[projectId]/releases/[releaseId]/page.tsx` | Full release detail page |
| `components/releases/assign-item-dialog.tsx` | Dialog to search and assign unassigned work items |

Features:
- Header card: name, status badge, description, release date, item count, progress bar
- Edit and Delete buttons in header; Delete routes back to releases list on success
- Work items section with TypeIcon, title, StatusBadge, PriorityIcon per row
- Remove item button per row (red hover, Trash2 icon)
- Assign Items dialog: fetches backlog filtered by `releaseId`; shows only unassigned items; search by title; Add button per item; multiple items can be added in one session
- Loading skeleton (header + 5 row skeletons)
- Error state for failed loads

---

## Acceptance Criteria — All Satisfied

| Criterion | Status |
|---|---|
| Release list shows all releases with status badges and progress | DONE |
| Create release works (name + description + date) | DONE |
| Release detail shows assigned items | DONE |
| Assign items works (search + add from unassigned backlog items) | DONE |
| Unassign/Remove items works | DONE |
| Delete release with confirmation dialog | DONE |
| `npm run build` passes with no errors | PASS |

---

## Design Decisions

- Used existing `ConfirmDialog` from `components/projects/` — no new shared confirm dialog needed.
- `AssignItemDialog` fetches backlog with `releaseId` filter (already assigned items) and an unfiltered fetch, then computes available items client-side by filtering out the IDs. This avoids needing a dedicated "unassigned items" API endpoint.
- The `BacklogItemDto` type is compatible with `WorkItemDto` for display purposes (it extends `WorkItemDto`).
- The release detail page uses `use(params)` (React 19 async params unwrapping) consistent with Next.js 15 App Router.
- `colorScheme: "dark"` on date inputs ensures the calendar picker respects the dark theme.
