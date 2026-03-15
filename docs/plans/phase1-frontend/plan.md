# Phase 1 Frontend — Implementation Plan

**Created:** 2026-03-15
**Branch:** `feat/phase-1-frontend`
**Scope:** Full Phase 1 frontend + backend integration
**Backend status:** 100% complete (29 endpoints, 6 controllers, SignalR hub ready)
**Frontend status:** Empty `src/apps/teamflow-web/` directory

---

## Architecture Decisions

**Stack:** Next.js 15 (App Router) + TypeScript + Tailwind CSS 4 + shadcn/ui
**Data fetching:** TanStack Query v5 (server state) + Axios (HTTP client)
**Client state:** Zustand (theme, sidebar, filters, UI ephemeral state)
**Real-time:** @microsoft/signalr (invalidate TanStack Query caches on events)
**Drag-drop:** @dnd-kit (Kanban cards, backlog reorder)
**Fonts:** Syne (headings), DM Sans (body), DM Mono (monospace)
**Theme:** CSS custom properties matching prototype tokens, dark/light toggle persisted in localStorage

**API base URL:** `http://localhost:5000/api/v1` (configurable via `NEXT_PUBLIC_API_URL`)
**SignalR endpoint:** `http://localhost:5000/hubs/teamflow`
**Auth:** Phase 1 uses no JWT (backend has `AlwaysAllowPermissionChecker` + `FakeCurrentUser`). Axios interceptor skeleton wired for Phase 2.

---

## Routing Structure

```
src/apps/teamflow-web/
  app/
    layout.tsx                    — Root layout (providers, fonts, theme)
    page.tsx                      — Redirect to /projects
    projects/
      page.tsx                    — Project list
      [projectId]/
        layout.tsx                — Project shell (sidebar tabs: Backlog, Board, Releases)
        page.tsx                  — Redirect to backlog
        backlog/
          page.tsx                — Backlog view
        board/
          page.tsx                — Kanban board
        releases/
          page.tsx                — Release list
          [releaseId]/
            page.tsx              — Release detail
        work-items/
          new/
            page.tsx              — Create work item (or dialog — TBD in phase)
          [workItemId]/
            page.tsx              — Work item detail
```

---

## Component Hierarchy

```
RootLayout
  ThemeProvider (Zustand + CSS vars)
  QueryClientProvider (TanStack)
  SignalRProvider (connection lifecycle)
  Toaster (sonner)

  AppShell
    TopBar (logo, breadcrumb, new-item button, theme toggle, avatar)
    ProjectLayout
      ProjectNav (tabs: Backlog | Board | Releases)

      BacklogPage
        BacklogToolbar (search, type/priority/release/blocked filters, view toggle)
        BacklogList
          EpicGroup (collapsible)
            BacklogRow (type icon, title, priority, assignee, points, link badges, release badge, blocked icon)
        BacklogDragLayer

      KanbanPage
        KanbanToolbar (filters: assignee, type, priority, swimlane toggle)
        KanbanBoard
          KanbanColumn (status header + count)
            KanbanCard (type, title, priority, assignee avatar, blocked icon, release badge)
        KanbanDragOverlay

      ReleasesPage
        ReleaseList
          ReleaseCard (name, date, status badge, progress bar, item counts)
        ReleaseDetailPage
          ReleaseHeader (name, status, date, progress)
          ReleaseItemList (assigned work items, remove button)
          AssignItemDialog

      WorkItemDetailPage
        WorkItemHeader (type icon, ID, title, status badge)
        WorkItemForm (title, description, priority, estimation, acceptance criteria)
        WorkItemSidebar (status, assignee, release, parent, dates)
        WorkItemTabs
          LinksTab (grouped by link type, add/remove links)
          ChildrenTab (child items list)

      CreateWorkItemDialog (modal — type, title, parent, priority, description)

      ConfirmBlockedDialog (lists blockers, confirm/cancel)

SharedComponents
  DataTable, Badge, StatusBadge, PriorityIcon, TypeIcon
  UserAvatar, SearchInput, FilterChip
  EmptyState, LoadingSkeleton, ErrorBoundary
  Pagination
```

---

## Data Flow

```
API (Axios) → TanStack Query (cache + dedup) → Components (read)
Components (mutate) → TanStack Query mutations → API → invalidate queries
SignalR events → invalidate specific TanStack Query keys → auto-refetch
Zustand → theme, sidebar state, active filters (no server data)
```

**Query key conventions:**
- `["projects"]` — project list
- `["projects", projectId]` — single project
- `["backlog", projectId, filters]` — backlog page
- `["kanban", projectId, filters]` — kanban board
- `["work-items", workItemId]` — single work item
- `["work-items", workItemId, "links"]` — work item links
- `["work-items", workItemId, "blockers"]` — blockers check
- `["releases", projectId]` — release list
- `["releases", releaseId]` — single release

**SignalR invalidation map:**
| Event | Invalidates |
|---|---|
| `WorkItem.*` | `["backlog", projectId]`, `["kanban", projectId]`, `["work-items", itemId]` |
| `WorkItem.LinkAdded/Removed` | Also `["work-items", itemId, "links"]` |
| `Release.*` | `["releases", projectId]`, `["releases", releaseId]` |

---

## Implementation Phases

### Phase A — Foundation (1 implementation session)

**Goal:** Running Next.js app with design system, API client, providers, layout shell.

**FILE OWNERSHIP:** All files under `src/apps/teamflow-web/`

#### Tasks

**A1. Next.js project scaffold**
- `npx create-next-app@latest teamflow-web` in `src/apps/` with TypeScript, Tailwind, App Router, src/ disabled (flat app/)
- Configure `next.config.ts`: API proxy rewrite to `localhost:5000`
- Add `.env.local`: `NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1`, `NEXT_PUBLIC_SIGNALR_URL=http://localhost:5000/hubs/teamflow`
- Files: `package.json`, `next.config.ts`, `tsconfig.json`, `tailwind.config.ts`, `.env.local`, `.env.example`

**A2. Design tokens + Tailwind theme**
- Extract CSS custom properties from prototype into `app/globals.css` (dark default + `[data-theme="light"]`)
- Configure Tailwind to use CSS vars for colors
- Import Google Fonts: Syne, DM Sans, DM Mono
- Files: `app/globals.css`, `tailwind.config.ts`

**A3. shadcn/ui setup**
- `npx shadcn@latest init` — configure with Tailwind CSS vars
- Install base components: Button, Badge, Dialog, DropdownMenu, Input, Select, Tabs, Tooltip, Skeleton, Separator, ScrollArea, Sheet
- Files: `components.json`, `lib/utils.ts`, `components/ui/*`

**A4. API client (Axios)**
- Create Axios instance with base URL, correlation ID header, request/response interceptors
- JWT interceptor skeleton (reads token from cookie/localStorage — no-op in Phase 1)
- Error interceptor: parse ProblemDetails, surface to caller
- Files: `lib/api/client.ts`, `lib/api/types.ts` (shared DTOs: `ProjectDto`, `WorkItemDto`, `ReleaseDto`, `KanbanBoardDto`, `PaginatedResponse<T>`, `ProblemDetails`)

**A5. TanStack Query + Zustand providers**
- QueryClientProvider with default stale time (30s), retry (1)
- Zustand stores: `useThemeStore` (dark/light, persisted), `useSidebarStore` (collapsed)
- Files: `lib/providers.tsx`, `lib/stores/theme-store.ts`, `lib/stores/sidebar-store.ts`, `lib/query-client.ts`

**A6. Layout shell**
- Root layout: providers, fonts, html theme attribute from Zustand
- TopBar: logo (TF icon + "TeamFlow"), breadcrumb slot, right section (theme toggle, avatar placeholder)
- No sidebar in Phase 1 (project nav tabs serve this purpose)
- Files: `app/layout.tsx`, `components/layout/top-bar.tsx`, `components/layout/breadcrumb.tsx`, `components/layout/theme-toggle.tsx`

**A7. Shared UI components**
- `TypeIcon` — colored icon per WorkItemType (story=blue, task=green, bug=red, spike=violet, epic=orange)
- `PriorityIcon` — arrow icons per priority level
- `StatusBadge` — colored badge per WorkItemStatus
- `UserAvatar` — initials circle with color
- `EmptyState` — icon + message + optional action
- `LoadingSkeleton` — pulse skeleton matching card layout
- `Pagination` — page controls using API pagination shape
- Files: `components/shared/*`

**Acceptance:** `npm run dev` shows layout shell with theme toggle. No API calls yet.

---

### Phase B — Projects Page (1 session)

**Goal:** Project CRUD, list with search/filter/pagination.

**FILE OWNERSHIP:** `app/projects/`, `lib/api/projects.ts`, `lib/hooks/use-projects.ts`

#### Tasks

**B1. Project API hooks**
- `getProjects(params)` — GET `/projects` with query params
- `getProject(id)` — GET `/projects/{id}`
- `createProject(data)` — POST `/projects`
- `updateProject(id, data)` — PUT `/projects/{id}`
- `archiveProject(id)` — POST `/projects/{id}/archive`
- `deleteProject(id)` — DELETE `/projects/{id}`
- TanStack Query hooks: `useProjects`, `useProject`, `useCreateProject`, `useUpdateProject`, `useArchiveProject`, `useDeleteProject`
- Files: `lib/api/projects.ts`, `lib/hooks/use-projects.ts`

**B2. Projects list page**
- Search input, status filter (All/Active/Archived), pagination
- Project cards: name, description, status badge, epic count, open item count, dates
- Create project dialog (name + description)
- Edit project dialog
- Archive/Delete with confirmation
- Empty state when no projects
- Loading skeleton
- Files: `app/projects/page.tsx`, `components/projects/project-card.tsx`, `components/projects/create-project-dialog.tsx`, `components/projects/edit-project-dialog.tsx`

**B3. Project layout + nav**
- `[projectId]/layout.tsx` — fetches project, provides context, renders nav tabs
- Nav tabs: Backlog | Board | Releases (match prototype `.nav-tabs` style)
- Breadcrumb: "ProjectName > Backlog" etc.
- Files: `app/projects/[projectId]/layout.tsx`, `components/projects/project-nav.tsx`

**Acceptance:** Create a project, see it in the list. Click into it, see nav tabs. Edit/archive/delete work.

---

### Phase C — Backlog View (1 session)

**Goal:** Full backlog with hierarchy, filters, blocked icons, release badges, reorder.

**FILE OWNERSHIP:** `app/projects/[projectId]/backlog/`, `lib/api/backlog.ts`, `lib/hooks/use-backlog.ts`, `components/backlog/*`

#### Tasks

**C1. Backlog API hooks**
- `getBacklog(params)` — GET `/backlog` with full filter matrix
- `reorderBacklog(data)` — POST `/backlog/reorder`
- TanStack Query: `useBacklog`, `useReorderBacklog`
- Files: `lib/api/backlog.ts`, `lib/hooks/use-backlog.ts`

**C2. Work Item API hooks** (shared across pages)
- All CRUD + status + assign + links + blockers endpoints
- TanStack mutations with optimistic updates where safe
- Files: `lib/api/work-items.ts`, `lib/hooks/use-work-items.ts`

**C3. Backlog toolbar**
- Search input with debounce (300ms)
- Filter chips: Type (Story/Task/Bug/Spike), Priority, Assignee, Release, Blocked-only
- View toggle: Epics (grouped) / Flat
- "New Item" button
- Files: `components/backlog/backlog-toolbar.tsx`, `lib/stores/backlog-filter-store.ts`

**C4. Backlog list**
- Epic groups: collapsible, colored dot, name, item count, total points
- Backlog row per item: type icon, ID (mono), title, priority icon, assignee avatar, points badge, link badges (blocked=red, relates=blue, depends=yellow), release badge (violet)
- Blocked icon (red circle) on items with unresolved blockers — tooltip lists blocker titles
- Click row opens work item detail
- Hover shows quick actions
- Files: `components/backlog/backlog-list.tsx`, `components/backlog/epic-group.tsx`, `components/backlog/backlog-row.tsx`

**C5. Backlog drag-drop reorder**
- @dnd-kit sortable for reorder within backlog
- On drop: call `POST /backlog/reorder` with new sort orders
- Drag overlay styled as card ghost
- Files: `components/backlog/backlog-dnd-provider.tsx`

**C6. Create work item dialog**
- Modal form: type select, title, parent (optional — dropdown of epics/stories in project), priority, description
- Calls POST `/workitems`, invalidates backlog query on success
- Toast on success/error
- Files: `components/work-items/create-work-item-dialog.tsx`

**Acceptance:** Backlog loads with hierarchy. Filters work. Reorder via drag-drop persists. Blocked icons show on correct items. Release badges visible.

---

### Phase D — Kanban Board (1 session)

**Goal:** Drag-drop Kanban with status columns, filters, blocked icons.

**FILE OWNERSHIP:** `app/projects/[projectId]/board/`, `lib/api/kanban.ts`, `lib/hooks/use-kanban.ts`, `components/kanban/*`

#### Tasks

**D1. Kanban API hooks**
- `getKanbanBoard(params)` — GET `/kanban` with filters
- TanStack Query: `useKanbanBoard`
- Files: `lib/api/kanban.ts`, `lib/hooks/use-kanban.ts`

**D2. Kanban toolbar**
- Filter chips: assignee, type, priority, sprint, release
- Swimlane toggle: None / By Assignee / By Epic
- Files: `components/kanban/kanban-toolbar.tsx`

**D3. Kanban board**
- Four columns: ToDo, InProgress, InReview, Done
- Column header: status name, item count
- Cards: type icon, title, priority icon, assignee avatar, blocked icon, release badge, parent title (if Story/Task)
- @dnd-kit: drag card between columns
- On drop: call `POST /workitems/{id}/status` with new status
- Files: `components/kanban/kanban-board.tsx`, `components/kanban/kanban-column.tsx`, `components/kanban/kanban-card.tsx`

**D4. Blocked item confirm dialog**
- When dropping a blocked item to InProgress: show confirm dialog listing blockers (from `GET /workitems/{id}/blockers`)
- User can confirm (override) or cancel
- Files: `components/kanban/confirm-blocked-dialog.tsx`

**D5. Kanban drag overlay**
- Ghost card follows cursor during drag
- Column highlights on drag-over
- Files: `components/kanban/kanban-dnd-provider.tsx`

**Acceptance:** Four columns render. Drag card between columns updates status. Blocked items show confirm dialog. Filters narrow visible cards.

---

### Phase E — Work Item Detail (1 session)

**Goal:** Full work item view/edit with links tab and assign/unassign.

**FILE OWNERSHIP:** `app/projects/[projectId]/work-items/`, `components/work-items/*`

#### Tasks

**E1. Work item detail page**
- Header: type icon, ID, title (editable inline), status badge
- Main area: description (textarea), acceptance criteria, estimation
- Sidebar: status (dropdown to change), priority (dropdown), assignee (set/clear), release badge, parent link, created/updated dates
- Save button for field edits (PUT `/workitems/{id}`)
- Files: `app/projects/[projectId]/work-items/[workItemId]/page.tsx`, `components/work-items/work-item-header.tsx`, `components/work-items/work-item-form.tsx`, `components/work-items/work-item-sidebar.tsx`

**E2. Status change**
- Dropdown with all valid statuses
- Calls `POST /workitems/{id}/status`
- If item is blocked and target is InProgress: show confirm dialog
- Files: `components/work-items/status-select.tsx`

**E3. Assign / Unassign**
- Assignee picker (dropdown of seed users for Phase 1)
- Assign: `POST /workitems/{id}/assign`
- Unassign: `POST /workitems/{id}/unassign`
- Files: `components/work-items/assignee-picker.tsx`

**E4. Links tab**
- List links grouped by type (Blocks, Is Blocked By, Relates To, etc.)
- Each link shows: target item type icon, ID, title, status badge
- Remove link button: `DELETE /workitems/{id}/links/{linkId}`
- Add link dialog: search work items, select link type, confirm
- Circular dependency error displayed from API ProblemDetails
- Files: `components/work-items/links-tab.tsx`, `components/work-items/add-link-dialog.tsx`

**E5. Children tab**
- List child items with type icon, title, status, assignee
- Click navigates to child detail
- "Add child" button opens create dialog with parent pre-set
- Files: `components/work-items/children-tab.tsx`

**E6. Delete work item**
- Confirm dialog warning about cascade soft-delete of children
- `DELETE /workitems/{id}`
- Redirect to backlog on success
- Files: `components/work-items/delete-work-item-dialog.tsx`

**Acceptance:** View/edit all fields. Change status. Assign/unassign. Add/remove links. Circular block error shown. Delete cascades reflected.

---

### Phase F — Releases (1 session)

**Goal:** Release CRUD, assign/unassign items, release badges flow.

**FILE OWNERSHIP:** `app/projects/[projectId]/releases/`, `lib/api/releases.ts`, `lib/hooks/use-releases.ts`, `components/releases/*`

#### Tasks

**F1. Release API hooks**
- All release endpoints + assign/unassign item
- TanStack Query hooks
- Files: `lib/api/releases.ts`, `lib/hooks/use-releases.ts`

**F2. Release list page**
- Cards: name, description, release date, status badge (Unreleased=green, Overdue=red, Released=gray), progress bar (done/total), item counts by status
- Create release dialog
- Files: `app/projects/[projectId]/releases/page.tsx`, `components/releases/release-card.tsx`, `components/releases/create-release-dialog.tsx`

**F3. Release detail page**
- Header: name, status badge, release date, progress bar
- Item list: assigned work items with type icon, title, status, priority
- Remove item button
- Assign item dialog: search/select unassigned items
- Edit release dialog
- Delete release with confirmation
- Files: `app/projects/[projectId]/releases/[releaseId]/page.tsx`, `components/releases/release-detail.tsx`, `components/releases/assign-item-dialog.tsx`

**Acceptance:** Create release. Assign items. Release badge appears on backlog rows. Remove items. Delete release.

---

### Phase G — SignalR Real-time (1 session)

**Goal:** Two tabs stay in sync. All mutations broadcast and reflected.

**FILE OWNERSHIP:** `lib/signalr/*`, `lib/providers.tsx` (SignalR additions)

#### Tasks

**G1. SignalR connection provider**
- `HubConnectionBuilder` with auto-reconnect, no JWT in Phase 1
- Note: Hub has `[Authorize]` — need to either: (a) remove `[Authorize]` from hub for Phase 1, or (b) pass a dummy token. Decision: **backend change** — add conditional `[AllowAnonymous]` for dev or remove `[Authorize]` in Phase 1. Flag for human review.
- Connection lifecycle: connect on mount, reconnect on disconnect, disconnect on unmount
- Join project group on project page mount, leave on unmount
- Files: `lib/signalr/signalr-provider.tsx`, `lib/signalr/connection.ts`

**G2. Event-to-query invalidation**
- Listen for SignalR events by type
- Map event types to TanStack Query keys for invalidation
- `WorkItem.Created/StatusChanged/Assigned/etc.` → invalidate backlog + kanban + work-item queries
- `Release.ItemAssigned/Created/etc.` → invalidate release queries
- Files: `lib/signalr/event-handlers.ts`

**G3. Toast notifications for remote changes**
- When another tab/user triggers a change, show subtle toast: "Work item SX-101 updated"
- Distinguish local mutations (no toast) from remote events (toast)
- Files: `lib/signalr/toast-notifications.ts`

**BACKEND CHANGE REQUIRED:** Remove `[Authorize]` from `TeamFlowHub` for Phase 1 (or make it conditional). This is a one-line change in `src/apps/TeamFlow.Api/Hubs/TeamFlowHub.cs`. Phase 2 re-adds it.

**Acceptance:** Open two tabs on same project. Create item in tab A, appears in tab B within 2 seconds. Change status in Kanban tab A, reflected in Backlog tab B.

---

### Phase H — Polish + Integration Testing (1 session)

**Goal:** Visual fidelity with prototypes, error handling, loading states, dark/light mode.

**FILE OWNERSHIP:** All frontend files (cross-cutting polish)

#### Tasks

**H1. Dark/Light mode**
- Verify all components render correctly in both modes
- Theme toggle: instant switch, persisted in localStorage, no flicker (SSR-safe via `<script>` in `<head>`)
- CSS var values match prototype exactly (dark: `--accent: #6ee7b7`, light: `--accent: #059669`)
- Files: `app/layout.tsx` (inline script), `app/globals.css`

**H2. Loading states**
- Skeleton loaders on: project list, backlog, kanban board, work item detail, release list
- Match card shapes for skeletons
- Files: `components/shared/loading-skeleton.tsx` (variations per page)

**H3. Error handling**
- Error boundary at project layout level
- API error display: parse ProblemDetails, show toast or inline error
- 404 pages for missing project/work-item/release
- Network error retry UI
- Files: `app/projects/[projectId]/error.tsx`, `app/projects/[projectId]/not-found.tsx`, `components/shared/error-display.tsx`

**H4. Toast system**
- Install `sonner` for toast notifications
- Success toasts on: create, update, delete, assign, link operations
- Error toasts for API failures with ProblemDetails detail message
- Files: `components/layout/toaster.tsx`

**H5. Visual fidelity pass**
- Compare each page against prototype HTML screenshots
- Match: spacing, font sizes, border radius, shadow values, color tokens
- Ensure card hover effects, animations (fadeUp), transitions match prototype
- Files: various component tweaks

**H6. Responsive basics**
- Kanban columns stack on narrow viewport
- Backlog hides non-essential columns on mobile
- Sidebar collapses
- Files: component responsive classes

**Acceptance:** Both modes pixel-match prototypes. All pages have loading/error states. Toasts fire on all mutations. No unstyled flash on load.

---

## File Tree Summary

```
src/apps/teamflow-web/
  app/
    globals.css                         -- Design tokens (dark + light)
    layout.tsx                          -- Root layout + providers
    page.tsx                            -- Redirect to /projects
    projects/
      page.tsx                          -- Project list
      [projectId]/
        layout.tsx                      -- Project layout + nav
        page.tsx                        -- Redirect to backlog
        backlog/page.tsx
        board/page.tsx
        releases/
          page.tsx
          [releaseId]/page.tsx
        work-items/
          new/page.tsx
          [workItemId]/page.tsx
        error.tsx
        not-found.tsx
  components/
    layout/
      top-bar.tsx
      breadcrumb.tsx
      theme-toggle.tsx
      toaster.tsx
    shared/
      type-icon.tsx
      priority-icon.tsx
      status-badge.tsx
      user-avatar.tsx
      empty-state.tsx
      loading-skeleton.tsx
      error-display.tsx
      pagination.tsx
      search-input.tsx
      filter-chip.tsx
    ui/                                 -- shadcn/ui components
    projects/
      project-card.tsx
      project-nav.tsx
      create-project-dialog.tsx
      edit-project-dialog.tsx
    backlog/
      backlog-toolbar.tsx
      backlog-list.tsx
      epic-group.tsx
      backlog-row.tsx
      backlog-dnd-provider.tsx
    kanban/
      kanban-toolbar.tsx
      kanban-board.tsx
      kanban-column.tsx
      kanban-card.tsx
      kanban-dnd-provider.tsx
      confirm-blocked-dialog.tsx
    work-items/
      create-work-item-dialog.tsx
      work-item-header.tsx
      work-item-form.tsx
      work-item-sidebar.tsx
      status-select.tsx
      assignee-picker.tsx
      links-tab.tsx
      add-link-dialog.tsx
      children-tab.tsx
      delete-work-item-dialog.tsx
    releases/
      release-card.tsx
      create-release-dialog.tsx
      release-detail.tsx
      assign-item-dialog.tsx
  lib/
    utils.ts                            -- cn() helper from shadcn
    query-client.ts                     -- TanStack Query config
    providers.tsx                        -- All providers composed
    api/
      client.ts                         -- Axios instance + interceptors
      types.ts                          -- Shared DTOs
      projects.ts                       -- Project API functions
      work-items.ts                     -- Work item API functions
      backlog.ts                        -- Backlog API functions
      kanban.ts                         -- Kanban API functions
      releases.ts                       -- Release API functions
    hooks/
      use-projects.ts                   -- TanStack Query hooks
      use-work-items.ts
      use-backlog.ts
      use-kanban.ts
      use-releases.ts
    stores/
      theme-store.ts                    -- Zustand: dark/light
      sidebar-store.ts                  -- Zustand: collapsed
      backlog-filter-store.ts           -- Zustand: active filters
    signalr/
      connection.ts                     -- Hub connection factory
      signalr-provider.tsx              -- React context + lifecycle
      event-handlers.ts                 -- Event-to-query invalidation
      toast-notifications.ts            -- Remote change toasts
```

---

## Dependencies

```json
{
  "dependencies": {
    "next": "^15",
    "react": "^19",
    "react-dom": "^19",
    "@tanstack/react-query": "^5",
    "@tanstack/react-query-devtools": "^5",
    "axios": "^1",
    "zustand": "^5",
    "@microsoft/signalr": "^8",
    "@dnd-kit/core": "^6",
    "@dnd-kit/sortable": "^10",
    "@dnd-kit/utilities": "^3",
    "sonner": "^2",
    "class-variance-authority": "^0.7",
    "clsx": "^2",
    "tailwind-merge": "^3",
    "lucide-react": "^0.400"
  },
  "devDependencies": {
    "typescript": "^5.7",
    "@types/react": "^19",
    "@types/node": "^22",
    "tailwindcss": "^4",
    "@tailwindcss/postcss": "^4"
  }
}
```

---

## Backend Changes Required

1. **Remove `[Authorize]` from `TeamFlowHub`** — Phase 1 has no auth. Re-add in Phase 2. One-line change.
2. **Verify CORS allows `localhost:3000`** — Already configured in `appsettings.json`. Confirmed.
3. **No other backend changes** — all 29 endpoints are ready.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| SignalR `[Authorize]` blocks anonymous frontend | Remove attribute for Phase 1; re-add Phase 2 |
| @dnd-kit complexity for Kanban + Backlog | Implement Kanban first (simpler columns), then backlog reorder |
| Prototype visual fidelity | Extract exact CSS vars from prototype HTML; dedicated polish phase |
| Large backlog pagination perf | TanStack Query handles caching; API already paginated |

---

## Session Execution Order

| Session | Phase | Est. Files | Depends On | Status |
|---|---|---|---|---|
| 1 | A — Foundation | ~25 | Nothing | completed |
| 2 | B — Projects | ~10 | A | pending |
| 3 | C — Backlog | ~12 | A, B | pending |
| 4 | D — Kanban | ~10 | A, C2 (work item hooks) | pending |
| 5 | E — Work Item Detail | ~12 | A, C2 | pending |
| 6 | F — Releases | ~8 | A, C2 | pending |
| 7 | G — SignalR | ~5 | A through F | pending |
| 8 | H — Polish | ~varies | All above |

Phases C, D, E, F can run in any order after B completes (they share C2 work item hooks, which should be built first in C).

---

## Success Criteria Checklist

- [ ] Project CRUD works end-to-end
- [ ] Epic > Story > Task hierarchy visible in backlog
- [ ] Delete Epic — children disappear from backlog
- [ ] Assign/Unassign reflected on backlog and detail
- [ ] Create "A blocks B" — B shows blocked icon + "is blocked by A" in links tab
- [ ] Delete link from A — reverse disappears from B
- [ ] Circular block attempt — error toast from API
- [ ] Blocked item dragged to InProgress — confirm dialog lists blockers
- [ ] Release badge on backlog row after assignment
- [ ] Loading states on all pages
- [ ] Error handling with ProblemDetails
- [ ] Dark mode renders correctly
- [ ] Light mode renders correctly
- [ ] Two tabs: change in one, other updates (SignalR)
- [ ] Kanban drag-drop updates status
- [ ] Backlog filters work (type, priority, release, blocked, search)
- [ ] Backlog reorder via drag-drop
