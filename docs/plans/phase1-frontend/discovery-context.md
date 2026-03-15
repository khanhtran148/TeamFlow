---
created: 2026-03-15
feature: Phase 1 Frontend — Full Build + Backend Integration
---

# Discovery Context

## Requirements

Phase 1 backend is 100% complete (29 endpoints, 124 tests, 6 controllers). The frontend directory (`src/apps/teamflow-web/`) is empty. Build the complete Phase 1 frontend and integrate with the existing backend API.

### Backend Endpoints Available
- **Projects:** CRUD, list with filter/search/pagination, archive, soft-delete
- **Work Items:** Full CRUD, hierarchy (Epic > Story > Task/Bug/Spike), status transitions, assign/unassign, move
- **Backlog:** Paginated/filtered view, reorder
- **Kanban:** Status-grouped board with filters
- **Releases:** CRUD, assign/unassign items
- **Linking:** Add/remove links (6 types), bidirectional, circular detection, blockers check

### HTML Prototypes
- `docs/prototypes/backlog-sprint-planning.html` (dark mode)
- `docs/prototypes/backlog-sprint-light-mode.html` (light mode)
- Design tokens: Syne (headings), DM Sans (body), DM Mono (mono), green accent (#6ee7b7 dark / #059669 light)

## Scope
**Fullstack** — Frontend + API integration (backend already built)

## UI Library
**Tailwind CSS + shadcn/ui** (Radix primitives) — match prototype design tokens

## Tech Stack (from CLAUDE.md)
- Next.js (App Router)
- TanStack Query (data fetching + caching)
- Zustand (client state)
- Axios with JWT interceptor (Phase 2 auth-ready)
- SignalR client (real-time)

## Success Criteria — Full Phase 1 AC Match
- [ ] Project → Epic → Story → Task — works end-to-end in UI
- [ ] Delete Epic → children soft-deleted, reflected in UI
- [ ] Assign → displayed on Backlog and Detail immediately
- [ ] Unassign → unassigned state, no other field affected
- [ ] Create A blocks B → B shows "is blocked by A" automatically
- [ ] Delete link from A → reverse disappears from B
- [ ] Circular block attempt → UI shows clear error from API
- [ ] Blocked item moved → confirm dialog lists blockers
- [ ] Release badge appears on Backlog row after assignment
- [ ] Each page has proper loading states and error handling
- [ ] Both dark and light mode render correctly
- [ ] Two browser tabs: change in one → other updates without refresh (SignalR)
- [ ] Drag-drop Kanban status updates
- [ ] Filters work on Backlog and Kanban
- [ ] Backlog reorder via drag-drop

## Real-time
**Include SignalR** — connect/reconnect/disconnect lifecycle, invalidate TanStack Query on events

## Pages Required
1. Projects list (with create/edit/archive/delete)
2. Project detail / overview
3. Backlog view (hierarchy, filters, reorder, blocked icons, release badges)
4. Kanban board (drag-drop, swimlanes, filters, blocked icons)
5. Work Item detail (full edit, status change, links tab, assign/unassign)
6. Work Item create dialog/page
7. Release list + detail (assign/unassign items)
8. Layout shell (sidebar, topbar, breadcrumb, dark/light toggle)
