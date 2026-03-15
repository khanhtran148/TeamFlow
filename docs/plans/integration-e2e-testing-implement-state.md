# Implementation State — Integration & E2E Testing Strategy

## Topic
Integration & E2E Testing Strategy: WebApplicationFactory + Respawn for backend, Playwright overhaul for frontend

## Discovery Context

- **Branch:** Continue on `feat/phase-3-sprint-hardening` (no new branch)
- **Requirements:** Full coverage strategy per ADR `docs/adrs/260315-integration-e2e-testing-strategy.md`
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)
- **Feature Scope:** Fullstack
- **Task Type:** feature

## Phase-Specific Context

- **Plan directory:** `docs/plans/integration-e2e-testing`
- **Plan source:** `docs/plans/integration-e2e-testing/plan.md`
- **ADR:** `docs/adrs/260315-integration-e2e-testing-strategy.md`

### Plan Summary

4 phases, Phases 1+3 parallel, Phases 2+4 parallel:

| Phase | Goal | Size | Dependencies |
|-------|------|------|-------------|
| 1 | Backend Infrastructure — PostgresFixture, WebAppFactory, Respawn, ApiIntegrationTestBase | M | None |
| 2 | Backend Coverage — Sprint endpoints, permission matrix, rate limiting, health checks | L | Phase 1 |
| 3 | Playwright Infrastructure — setup project, storageState, data-testid, unified fixtures | S | None |
| 4 | Playwright Coverage — Sprint E2E, visual regression, navigation, permission UI | M | Phase 3 |

### Key Decisions
- Respawn over EF recreate (faster DB reset)
- ICollectionFixture for shared Postgres container
- Real JWT generation in integration tests
- storageState over localStorage injection for Playwright auth
- data-testid scoped to interactive elements only

### User Modifications
None — follow plan as-is.
