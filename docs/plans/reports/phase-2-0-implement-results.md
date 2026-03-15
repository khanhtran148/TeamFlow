# Phase 2.0 — P1 Security Fixes & Playwright Setup — Results

**Date:** 2026-03-15
**Branch:** `feat/phase-2-auth`
**Status:** Complete

## Summary

All 5 sub-tasks completed with TFD compliance.

## Changes

### 2.0.1 — Secrets out of VCS
- Created `.env.example` with placeholder values (committed)
- Created `.env` with dev values (gitignored)
- Refactored `docker-compose.yml` to use `${VAR}` substitution — no hardcoded secrets
- Cleared sensitive values from `appsettings.json` (connection string, JWT secret, RabbitMQ creds)
- Added dev-only values to `appsettings.Development.json` (gitignored)
- `Program.cs` already had fail-fast on missing JWT secret — no change needed

### 2.0.2 — [Authorize] on all controllers
- Added `[Authorize]` attribute to `ApiControllerBase`
- All 5 controllers (Projects, WorkItems, Releases, Backlog, Kanban) inherit auth requirement
- **Tests added:** 6 (1 base class + 5 per-controller)

### 2.0.3 — SignalR group-join validation
- Refactored `TeamFlowHub` to sealed class with primary constructor
- Injected `IPermissionChecker` and `ICurrentUser`
- `JoinProject` now validates permission via `HasPermissionAsync(userId, projectId, Project_View)`
- All join methods now validate GUID format
- **Tests added:** 3 (permission granted, permission denied, invalid GUID)

### 2.0.4 — Auth rate limit to spec
- Changed from 5 req/min to 10 req/15 min per IP
- Added `Retry-After` header on 429 responses
- Exposed `AuthPolicyConfig` record for testability
- **Tests added:** 2 (permit limit, window duration)

### 2.0.5 — Playwright setup
- Installed `@playwright/test` as devDependency
- Created `playwright.config.ts` with Chromium project, webServer config
- Created test fixtures (`e2e/fixtures/auth.ts`) with registerUser/loginUser helpers
- Created page objects (`LoginPage`, `RegisterPage`)
- Created smoke test
- Added `e2e`, `e2e:ui`, `e2e:headed` scripts to package.json
- Added Playwright artifacts to `.gitignore`

## Test Results

| Suite | Passed | Failed | Total |
|-------|--------|--------|-------|
| Domain | 17 | 0 | 17 |
| Application | 109 | 0 | 109 |
| API | 19 | 0 | 19 |
| **Total** | **145** | **0** | **145** |

## New Tests Added: 11

## Human Review Needed
- **2.0.1:** Verify `.env` values and docker-compose variable substitution work correctly
- **2.0.3:** Verify SignalR permission check meets security requirements

## Unresolved Questions
None.
