# Phase 3: Playwright Infrastructure -- Implementation Report

**Date:** 2026-03-15
**Status:** COMPLETED
**Branch:** `feat/phase-3-sprint-hardening`
**Vitest TFD Status:** N/A (no logic-bearing frontend code in scope)

---

## Summary

All 4 tasks in Phase 3 (Playwright Infrastructure) are complete. The infrastructure adds global auth setup, data-testid attributes on interactive components, unified test fixtures with storageState awareness, and afterAll cleanup hooks.

---

## Task 3.1: Add setup project for global auth

**Status:** COMPLETED

### Files Modified
| File | Change |
|------|--------|
| `src/apps/teamflow-web/playwright.config.ts` | Added `setup` project + `storageState` to chromium project |
| `src/apps/teamflow-web/e2e/global-setup.ts` | Created: registers user, injects tokens, saves storageState |
| `src/apps/teamflow-web/.gitignore` | Added `/.auth/` to gitignore |

### Details
- Setup project uses `testMatch: /global-setup\.ts/` pattern
- Chromium project depends on `["setup"]` and uses `storageState: ".auth/user.json"`
- Global setup registers a fresh user, injects auth tokens into localStorage, and saves storageState
- `.auth/` directory is gitignored

---

## Task 3.2: Add data-testid to critical components

**Status:** COMPLETED

### Files Modified
| Component | File | Testids Added |
|-----------|------|---------------|
| SprintCard | `components/sprints/sprint-card.tsx` | `sprint-card-{id}` |
| SprintStatusBadge | `components/sprints/sprint-status-badge.tsx` | `sprint-status-{status}` |
| SprintFormDialog | `components/sprints/sprint-form-dialog.tsx` | `sprint-form-dialog`, `sprint-name-input`, `sprint-goal-input`, `sprint-start-date`, `sprint-end-date`, `sprint-submit-btn` |
| SprintPlanningBoard | `components/sprints/sprint-planning-board.tsx` | `sprint-planning-board`, `backlog-panel`, `sprint-panel` |
| CapacityForm | `components/sprints/capacity-form.tsx` | `capacity-form`, `capacity-member-{id}`, `capacity-save-btn` |
| BurndownChart | `components/sprints/burndown-chart.tsx` | `burndown-chart` |
| AddItemConfirmation | `components/sprints/add-item-confirmation.tsx` | `add-item-confirm-dialog`, `add-item-confirm-btn`, `add-item-cancel-btn` |
| ConfirmDialog | `components/projects/confirm-dialog.tsx` | Added `data-testid`, `confirmTestId`, `cancelTestId` props |
| TopBar | `components/layout/top-bar.tsx` | `top-bar`, `nav-projects` |
| UserMenu | `components/layout/user-menu.tsx` | `user-menu-btn`, `user-menu`, `logout-btn` |
| ThemeToggle | `components/layout/theme-toggle.tsx` | `theme-toggle` |
| Pagination | `components/shared/pagination.tsx` | `pagination`, `page-prev`, `page-next` |

### Deviations
- **`nav-teams`**: No teams navigation link exists in the current layout (TopBar). The layout only has a logo link to `/projects`. The `nav-teams` testid is deferred until a teams nav link is added to the layout.
- ConfirmDialog was extended with optional `data-testid`, `confirmTestId`, and `cancelTestId` props to support pass-through from AddItemConfirmation without modifying the alertdialog element directly.

---

## Task 3.3: Standardize test.extend fixtures

**Status:** COMPLETED

### Files Modified/Created
| File | Change |
|------|--------|
| `e2e/fixtures/index.ts` | Created: unified barrel with `test.extend` fixture, `sprintHelpers` fixture, re-exports for backward compatibility |
| `e2e/fixtures/auth.ts` | Modified: added `hasStorageState` fixture, storageState path awareness |
| `e2e/fixtures/sprint-helpers.ts` | Modified: now re-exports from `index.ts` for backward compatibility |

### Details
- `e2e/fixtures/index.ts` provides a unified `test` export with both `AuthFixtures` and `SprintHelpers`
- `sprintHelpers` fixture wraps all API helper functions with request context
- Legacy function exports (`registerUser`, `createProject`, etc.) are re-exported for backward compatibility so existing spec files continue to work unchanged
- `hasStorageState` fixture checks if `.auth/user.json` exists from global setup
- `deleteProject` helper added for cleanup

---

## Task 3.4: Add afterAll cleanup hooks

**Status:** COMPLETED

### Files Modified
| Spec File | Cleanup Added |
|-----------|---------------|
| `e2e/sprints/sprint-planning.spec.ts` | `test.afterAll` deletes project |
| `e2e/sprints/sprint-backlog.spec.ts` | `test.afterAll` deletes project |
| `e2e/sprints/burndown-chart.spec.ts` | `test.afterAll` deletes project |
| `e2e/releases/overdue-release.spec.ts` | `test.afterAll` deletes project |
| `e2e/work-items/stale-flag.spec.ts` | `test.afterAll` deletes project |

### Details
- All cleanup is best-effort: `deleteProject` catches errors silently
- Guard checks (`if (token && projectId)`) prevent cleanup from running if setup failed
- Spec files that don't create persistent test data (smoke, auth-flow, permissions, history, rate-limit, teams) were not modified as they don't accumulate data

---

## Verification

- **TypeScript:** `tsc --noEmit` passes with zero errors
- **Test Discovery:** `npx playwright test --list` discovers all 38 tests in 14 files
- **Setup Project:** Listed first: `[setup] > global-setup.ts:13:6 > authenticate`
- **Chromium Dependency:** All chromium tests correctly depend on setup project

---

## Unresolved Questions

1. **`nav-teams` data-testid:** No teams navigation link exists in the current TopBar layout. Will need to be added when a teams nav link is implemented.
2. **Existing spec file imports:** Existing spec files still import from `../fixtures/sprint-helpers` (backward compatible re-exports). Phase 4 can optionally migrate them to import from `../fixtures` directly.
