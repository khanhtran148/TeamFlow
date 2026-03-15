---
topic: Phase A Foundation — Next.js scaffold, design tokens, shadcn/ui, Axios client, providers, layout shell
task_type: feature
---

# Implementation State

## Discovery Context

- **Branch:** Create `feat/phase-1-frontend` from current branch `feat/phase-1-work-items`
- **Requirements:** Phase A of the Phase 1 Frontend plan — foundation setup only
- **Test DB Strategy:** N/A (frontend-only, no backend tests)
- **Task Type:** feature (new frontend app)

## Phase-Specific Context

### Plan Summary

Phase A delivers a running Next.js app with:

1. **A1 — Next.js project scaffold** in `src/apps/teamflow-web/` with TypeScript, Tailwind 4, App Router
2. **A2 — Design tokens** extracted from HTML prototypes into CSS custom properties (dark default + light mode)
3. **A3 — shadcn/ui setup** with base components (Button, Badge, Dialog, DropdownMenu, Input, Select, Tabs, Tooltip, Skeleton, Separator, ScrollArea, Sheet)
4. **A4 — Axios API client** with base URL, ProblemDetails error parsing, JWT interceptor skeleton
5. **A5 — TanStack Query + Zustand providers** (QueryClient config, theme store, sidebar store)
6. **A6 — Layout shell** (root layout with providers, TopBar with logo/breadcrumb/theme toggle)
7. **A7 — Shared UI components** (TypeIcon, PriorityIcon, StatusBadge, UserAvatar, EmptyState, LoadingSkeleton, Pagination)

### Key Decisions
- **Stack:** Next.js 15 + React 19 + TypeScript + Tailwind CSS 4 + shadcn/ui
- **Fonts:** Syne (headings), DM Sans (body), DM Mono (monospace) — from Google Fonts
- **Theme:** CSS custom properties, dark default, `[data-theme="light"]` override, persisted in localStorage
- **API base:** `http://localhost:5000/api/v1` via `NEXT_PUBLIC_API_URL`
- **SignalR:** `http://localhost:5000/hubs/teamflow` via `NEXT_PUBLIC_SIGNALR_URL`

### Plan Directory
`docs/plans/phase1-frontend`

### Plan Source
`docs/plans/phase1-frontend/plan.md` — Phase A section

### Acceptance Criteria
`npm run dev` shows layout shell with working theme toggle. No API calls yet.

### Shared DTO Types to Define
- `ProjectDto`, `WorkItemDto`, `WorkItemSummaryDto`, `BacklogItemDto`, `KanbanBoardDto`, `KanbanColumnDto`, `KanbanItemDto`, `ReleaseDto`
- `PaginatedResponse<T>`, `ProblemDetails`
- Enums: `WorkItemType`, `WorkItemStatus`, `Priority`, `LinkType`, `LinkScope`, `ProjectStatus`, `ReleaseStatus`
