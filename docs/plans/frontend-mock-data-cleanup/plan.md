# Plan: Replace Frontend Mock Data with Real API Calls

**Created:** 2026-03-16
**Scope:** Frontend-only (3 files to fix, 1 already fixed)

---

## Audit Results

Scanned all 100+ frontend files. Found **4 issues** — the rest are legitimate domain enums or test fixtures.

| # | File | Issue | Severity |
|---|------|-------|----------|
| 1 | `components/work-items/assignee-picker.tsx` | ~~SEED_USERS hardcoded~~ | **DONE** (fixed earlier) |
| 2 | `app/teams/page.tsx:16` | Hardcoded org ID `00000000-...0010` | HIGH |
| 3 | `components/projects/create-project-dialog.tsx:14` | Hardcoded org ID `00000000-...0001` | HIGH |
| 4 | `app/projects/[projectId]/backlog/page.tsx:74-83` | Placeholder blocker data | MEDIUM |

---

## Tasks

### Task 1: Replace hardcoded org ID in Teams page
**File:** `app/teams/page.tsx`
**Current:** `const DEFAULT_ORG_ID = "00000000-0000-0000-0000-000000000010"`
**Fix:** Get current user's organization from auth context or user profile API. Need to either:
- Add a `/api/v1/auth/me` or `/api/v1/users/me` endpoint that returns the user's org
- Or derive from the user's project memberships (fetch projects, extract org IDs)

### Task 2: Replace hardcoded org ID in Create Project dialog
**File:** `components/projects/create-project-dialog.tsx`
**Current:** `const DEFAULT_ORG_ID = "00000000-0000-0000-0000-000000000001"`
**Fix:** Same as Task 1 — use current user's org from context. If user belongs to multiple orgs, show org selector.

### Task 3: Replace placeholder blocker data in Backlog
**File:** `app/projects/[projectId]/backlog/page.tsx`
**Current:** Generic `{ blockerId: "unknown", title: "This item has unresolved blockers" }`
**Fix:** Fetch real blocker details from the work item links API. The endpoint `GET /api/v1/workitems/{id}/links` exists and returns linked items including blockers.

---

## Approach

### For Tasks 1 & 2: User Organization Context
The cleanest solution is a "current user profile" API or Zustand store that holds the user's organizations. Options:
- **Option A:** Add `GET /api/v1/users/me` endpoint returning user profile + org memberships
- **Option B:** Parse JWT claims (if org info is in the token)
- **Option C:** Fetch `/api/v1/projects` (already scoped to user) and extract unique org IDs

**Recommended: Option A** — cleanest, most reusable.

### For Task 3: Blocker Details
Use existing work item links data to show real blocker titles instead of generic placeholder.
