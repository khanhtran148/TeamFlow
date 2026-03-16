# Implement State: Phase 4 + 5 — Frontend Multi-Org UX + Onboarding

## Topic
Implement Phase 4 (Frontend Multi-Org UX) and Phase 5 (Onboarding Flow) of the org management feature.

## Discovery Context

- **Branch:** `feat/org-management-admin-bootstrap` (continuing)
- **Feature Scope:** Fullstack (Phase 5 has backend endpoints + both phases have frontend)
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)

## Phase 4: Frontend Multi-Org UX

### 4.1 API Client & Types
- `src/apps/teamflow-web/lib/api/types.ts` — Add `OrganizationDto` (with slug), `OrgRole`, `InvitationDto`, `OrganizationMemberDto`, `MyOrganizationDto` types. Add `systemRole` to `AuthUser` (if not already from Phase 1).
- `src/apps/teamflow-web/lib/api/organizations.ts` — CREATE: `listMyOrgs()`, `getOrgBySlug(slug)`, `createOrg()`, `updateOrg()`
- `src/apps/teamflow-web/lib/api/invitations.ts` — CREATE: `createInvitation()`, `acceptInvitation(token)`, `revokeInvitation()`, `listInvitations()`
- `src/apps/teamflow-web/lib/api/members.ts` — CREATE: stub for Phase 6

### 4.2 Auth & State Updates
- `src/apps/teamflow-web/lib/stores/auth-store.ts` — Verify `systemRole` parsing from JWT (may already be done in Phase 1)
- `src/apps/teamflow-web/lib/hooks/use-organizations.ts` — CREATE: TanStack Query hooks `useMyOrganizations()`, `useOrganizationBySlug(slug)`
- `src/apps/teamflow-web/lib/stores/org-store.ts` — CREATE: Zustand store for current org context (derived from URL slug)
- `src/apps/teamflow-web/lib/contexts/org-context.tsx` — CREATE: React context that reads slug from URL params

### 4.3 Route Restructure
Move all project/team routes under `/org/[slug]/...`:
- `src/apps/teamflow-web/app/org/[slug]/layout.tsx` — CREATE: Org-scoped layout, fetches org by slug, provides OrgContext
- `src/apps/teamflow-web/app/org/[slug]/projects/page.tsx` — Move from `app/projects/page.tsx`
- `src/apps/teamflow-web/app/org/[slug]/projects/[projectId]/...` — Move all project sub-routes
- `src/apps/teamflow-web/app/org/[slug]/teams/...` — Move team routes
- `src/apps/teamflow-web/app/org/[slug]/settings/page.tsx` — CREATE: Org settings page
- Old route files — Replace with redirect to `/org/{slug}/...`

### 4.4 Navigation Components
- `src/apps/teamflow-web/components/layout/org-switcher.tsx` — CREATE: Dropdown showing user's orgs
- `src/apps/teamflow-web/components/layout/top-bar.tsx` — MODIFY: Add org switcher
- Update breadcrumbs to include org name

### 4.5 Middleware & Guards
- `src/apps/teamflow-web/middleware.ts` — MODIFY: Handle `/org/` paths, redirect old `/projects/` URLs
- Auth guards — Handle org-scoped routes

### 4.6 Admin Dashboard — ALREADY COMPLETED in Phase 1
Skip this section entirely.

## Phase 5: Onboarding Flow

### 5.1 Backend: Pending Invitations for User (TFD)
Tests first:
- `tests/TeamFlow.Application.Tests/Features/Invitations/ListPendingForUserTests.cs` — Returns pending invitations matching user email; Excludes expired/revoked/accepted

Implementation:
- `src/core/TeamFlow.Application/Features/Invitations/ListPendingForUser/ListPendingForUserQuery.cs`
- `src/core/TeamFlow.Application/Features/Invitations/ListPendingForUser/ListPendingForUserHandler.cs` — Match by current user email, filter pending + not expired

### 5.2 Frontend: Onboarding Pages
- `src/apps/teamflow-web/app/onboarding/page.tsx` — Router: 0 orgs → no-orgs view, 1 org → redirect to /org/{slug}, N orgs → org picker
- `src/apps/teamflow-web/app/onboarding/no-orgs/page.tsx` — "No organizations yet" with pending invitations
- `src/apps/teamflow-web/app/onboarding/pick-org/page.tsx` — Org picker grid
- `src/apps/teamflow-web/components/onboarding/pending-invitations.tsx` — List pending invitations with accept buttons
- `src/apps/teamflow-web/components/onboarding/org-picker-card.tsx` — Card for org selection
- `src/apps/teamflow-web/app/invite/[token]/page.tsx` — Invitation acceptance deep link page

### Post-Login Redirect Logic
After login/register:
1. Call `GET /api/v1/me/organizations`
2. If 0 orgs → redirect to `/onboarding`
3. If 1 org → redirect to `/org/{slug}/projects`
4. If N orgs → redirect to `/onboarding/pick-org`

## API Endpoints (already implemented in backend Phases 1-3)
- `GET /api/v1/me/organizations` — List user's orgs (Phase 2)
- `GET /api/v1/organizations/by-slug/{slug}` — Get org by slug (Phase 2)
- `POST /api/v1/invitations/{token}/accept` — Accept invitation (Phase 3)
- `GET /api/v1/admin/organizations` — Admin list orgs (Phase 1)
- `GET /api/v1/admin/users` — Admin list users (Phase 1)

## Key Constraints
- Remove ALL hardcoded `DEFAULT_ORG_ID` references
- All project/team routes must be under `/org/[slug]/`
- Org context comes from URL (not cookies/state)
- Admin dashboard at `/admin` already exists from Phase 1

## Phase-Specific Context
- **Plan directory:** docs/plans/org-management-admin-bootstrap
- **Plan source:** docs/plans/org-management-admin-bootstrap/plan.md (Phases 4 + 5)
- **ADR:** docs/adrs/260316-org-management-admin-bootstrap.md
