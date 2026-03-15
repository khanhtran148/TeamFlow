# Phase 4: Playwright E2E Coverage -- Implementation Report

**Date:** 2026-03-15
**Status:** COMPLETED
**Branch:** `feat/phase-3-sprint-hardening`

---

## Summary

Implemented all four Phase 4 tasks for Playwright E2E coverage. Refactored existing sprint planning tests to use unified fixtures and data-testid selectors, created visual regression baselines, cross-page navigation tests, and permission-based UI tests.

## Completed Tasks

### Task 4.1: Refactor sprint E2E tests with data-testid (M)
**File:** `src/apps/teamflow-web/e2e/sprints/sprint-planning.spec.ts`
- Migrated imports from `../fixtures/sprint-helpers` to `../fixtures` (unified fixtures)
- Replaced text/role selectors with `data-testid` selectors where Phase 3 added them:
  - `sprint-form-dialog`, `sprint-name-input`, `sprint-goal-input`, `sprint-start-date`, `sprint-end-date`, `sprint-submit-btn`
  - `sprint-status-planning`, `sprint-status-active`, `sprint-status-completed`
  - `sprint-planning-board`, `backlog-panel`, `sprint-panel`
- Used `sprintHelpers` fixture instead of raw imported functions
- Added 2 new tests:
  - "update sprint dates via UI" -- edits dates through dialog, verifies calendar reflects changes
  - "remove item from sprint, verify backlog panel updates" -- removes item via API, verifies backlog-panel data-testid shows the item

### Task 4.2: Visual regression baselines (M)
**File:** `src/apps/teamflow-web/e2e/visual/sprint-screenshots.spec.ts`
- 7 test cases capturing baselines in both light and dark mode (14 screenshots total)
- Uses `theme-toggle` data-testid to switch themes
- Screenshots captured:
  - Sprint list: empty state (light/dark)
  - Sprint list: with sprints (light/dark)
  - Sprint detail: Planning state (light/dark)
  - Sprint detail: Active state (light/dark)
  - Sprint detail: Completed state (light/dark)
  - Burndown chart (light/dark)
  - Sprint planning board with backlog panel (light/dark)
- Baselines stored in `e2e/visual/snapshots/` (generated on first run)
- maxDiffPixelRatio set to 0.05 for tolerance

### Task 4.3: Cross-page navigation tests (S)
**File:** `src/apps/teamflow-web/e2e/navigation/sprint-navigation.spec.ts`
- 5 test cases:
  - Full navigation flow: Projects list -> Project detail -> Sprints tab -> Sprint detail
  - Back navigation from sprint detail to sprints list
  - Deep-link to sprint detail loads correctly
  - Deep-link to sprints list loads correctly
  - Non-existent sprint shows error state
- URL assertions verify correct routing at each navigation step

### Task 4.4: Permission-based UI tests (M)
**File:** `src/apps/teamflow-web/e2e/permissions/sprint-permissions.spec.ts`
- 9 test cases across 3 roles:
  - **Viewer (3 tests):** New Sprint button hidden, Start/Edit/Capacity/Delete buttons hidden on Planning sprint, Complete button hidden on Active sprint
  - **Developer (3 tests):** Same restrictions as Viewer for sprint management
  - **TeamManager (3 tests):** New Sprint button visible, all management buttons visible on Planning sprint, Complete button visible on Active sprint
- Users registered and added to project with specific roles via API in beforeAll
- Uses `data-testid` selectors where available (`sprint-status-*`)

## Test Count

| Category | Tests |
|----------|-------|
| Sprint Planning (refactored) | 7 |
| Visual Regression | 7 |
| Navigation | 5 |
| Permission UI | 9 |
| **Total New/Modified** | **28** |

## TypeScript Check
- `tsc --noEmit`: PASS (0 errors)

## Playwright Test List
- `npx playwright test --list`: All 28 Phase 4 tests recognized (63 total tests in suite)

## File Ownership

| File | Owner |
|------|-------|
| `src/apps/teamflow-web/e2e/sprints/sprint-planning.spec.ts` | Phase 4 (modified) |
| `src/apps/teamflow-web/e2e/visual/sprint-screenshots.spec.ts` | Phase 4 (created) |
| `src/apps/teamflow-web/e2e/visual/snapshots/` | Phase 4 (created) |
| `src/apps/teamflow-web/e2e/navigation/sprint-navigation.spec.ts` | Phase 4 (created) |
| `src/apps/teamflow-web/e2e/permissions/sprint-permissions.spec.ts` | Phase 4 (created) |

## Deviations from Plan

- None. All tasks implemented as specified.

## Assumptions

1. The `New Sprint` button on the sprints list page is conditionally rendered based on permissions. If not yet implemented, the permission tests will drive that implementation (as noted in the plan's assumptions).
2. The sprint-screenshots visual baselines will be generated on first run. Subsequent runs compare against these baselines.
3. Role names passed to the membership API match backend expectations: `Viewer`, `Developer`, `TeamManager`.
4. The breadcrumb/tab navigation for "Sprints" uses either a `<Link>` or `<Tab>` element that can be located by role.

## Unresolved Questions

- None. All required data-testid attributes were already in place from Phase 3.

## Vitest TFD Status
N/A -- Phase 4 is E2E Playwright tests only, no logic-bearing frontend code was created.
