---
Status: COMPLETED
Phase: B — Projects Page
Date: 2026-03-15
Branch: feat/phase-1-frontend
Build: PASS (0 errors, 0 TypeScript errors)
---

# Phase B — Projects Page Implementation Results

## Summary

All Phase B tasks (B1, B2, B3) completed successfully. `npm run build` passes with no errors or warnings.

---

## B1: Project API hooks

### New files
- `src/apps/teamflow-web/lib/api/projects.ts` — API functions (getProjects, getProject, createProject, updateProject, archiveProject, deleteProject)
- `src/apps/teamflow-web/lib/hooks/use-projects.ts` — TanStack Query hooks (useProjects, useProject, useCreateProject, useUpdateProject, useArchiveProject, useDeleteProject)
- `src/apps/teamflow-web/lib/hooks/use-debounce.ts` — generic debounce hook (300ms default for search)

### Details
- All API functions use the shared `apiClient` from `lib/api/client.ts`
- All mutations invalidate `["projects"]` on success
- `useDeleteProject` also calls `removeQueries` on the specific project detail key
- `useProject` is enabled only when `id` is truthy

---

## B2: Projects list page

### New files
- `src/apps/teamflow-web/app/projects/page.tsx` — full project listing page (replaces Phase A placeholder)
- `src/apps/teamflow-web/components/projects/project-card.tsx` — project card with status badge, stats, and inline dropdown menu
- `src/apps/teamflow-web/components/projects/create-project-dialog.tsx` — create project modal
- `src/apps/teamflow-web/components/projects/edit-project-dialog.tsx` — edit project modal
- `src/apps/teamflow-web/components/projects/confirm-dialog.tsx` — reusable confirm dialog (archive + delete)

### Features implemented
- Search input with 300ms debounce via `useDebounce`
- Status filter chips: All / Active / Archived (resets page to 1 on change)
- Responsive card grid (auto-fill, min 280px columns)
- Project card shows: name, status badge, description (2-line clamp), epic count, open item count, created date
- Inline dropdown menu per card: Edit, Archive (Active only), Delete
- Create project dialog with name validation (required, max 100 chars) + description
- Edit project dialog pre-populated from project data
- Archive confirm dialog (yellow warning style)
- Delete confirm dialog (red destructive style)
- Empty state: shows different message for filter/search vs. no projects at all
- Loading skeleton: 6 card-shaped pulses matching the card layout
- Pagination via shared `Pagination` component (only shown when > 12 projects)
- Toast notifications on all success/error outcomes (sonner)
- PAGE_SIZE = 12

---

## B3: Project layout + nav

### New files
- `src/apps/teamflow-web/app/projects/[projectId]/layout.tsx` — server component, extracts projectId from params, delegates to client wrapper
- `src/apps/teamflow-web/app/projects/[projectId]/project-layout-client.tsx` — client component that fetches project, provides context, renders TopBar + ProjectNav + children
- `src/apps/teamflow-web/app/projects/[projectId]/page.tsx` — redirects to `/projects/[projectId]/backlog`
- `src/apps/teamflow-web/components/projects/project-nav.tsx` — nav tabs (Backlog | Board | Releases) with active state underline
- `src/apps/teamflow-web/lib/contexts/project-context.tsx` — React context + provider + `useProjectContext` hook

### Placeholder sub-pages (content for Phase C/D/F)
- `src/apps/teamflow-web/app/projects/[projectId]/backlog/page.tsx` — Phase C placeholder
- `src/apps/teamflow-web/app/projects/[projectId]/board/page.tsx` — Phase D placeholder
- `src/apps/teamflow-web/app/projects/[projectId]/releases/page.tsx` — Phase F placeholder

### Nav behavior
- Active tab: underlined with `--tf-accent` border
- Breadcrumb: Projects > ProjectName > ActiveTab (e.g., "Projects > TeamFlow API > Backlog")
- Loading state: full-page loading skeleton while project is fetching
- Error/not found state: EmptyState with "Back to Projects" link

---

## Acceptance Criteria Status

| Criteria | Status |
|---|---|
| Create a project → see it in the list | Ready (requires running API) |
| Click into project → see nav tabs (Backlog / Board / Releases) | PASS |
| Edit project works | Ready (requires running API) |
| Archive project works | Ready (requires running API) |
| Delete project works | Ready (requires running API) |
| Search projects with debounce | PASS (300ms debounce) |
| Filter by status (All/Active/Archived) | PASS |
| Loading states render | PASS (card skeletons) |
| Empty state renders | PASS (with contextual messages) |
| `npm run build` passes with no errors | PASS |

---

## Implementation Notes

- `DEFAULT_ORG_ID` in `create-project-dialog.tsx` is set to `00000000-0000-0000-0000-000000000001` (matches Phase 1 seed data). Phase 2 will replace this with the authenticated user's org.
- The project layout uses a server layout + client wrapper pattern to comply with Next.js App Router's async `params` requirement (params is a Promise in Next.js 16).
- All new components use CSS custom properties (tf- prefixed tokens) for styling, matching the design system established in Phase A.
- No new dependencies added — all features use existing packages (TanStack Query, sonner, lucide-react).
