# ADR: Integration & E2E Testing Strategy

**Date:** 2026-03-15
**Status:** Accepted
**Deciders:** Development team

## Context

TeamFlow has 6 backend test projects and 13 Playwright E2E specs, but significant gaps exist:
- WebApplicationFactory is installed but unused — no HTTP-level integration tests
- No repository integration tests beyond PermissionChecker
- Each test class starts its own Postgres container (slow at scale)
- Playwright E2E tests have inconsistent fixtures, no shared auth state, no `data-testid` selectors
- Zero frontend component/unit tests

We need a long-term testing strategy that scales across all future phases, runs reliably in CI, and uses real infrastructure (Postgres, RabbitMQ) over mocks.

## Decision

### Backend Integration Tests: WebApplicationFactory + ICollectionFixture + Respawn

- Use `WebApplicationFactory<Program>` for HTTP-level integration tests
- Share a single Postgres Testcontainer per xUnit collection via `ICollectionFixture<PostgresFixture>`
- Use **Respawn** to reset DB state between tests (no container restart, no test pollution)
- Test all endpoints at the wire level: status codes, ProblemDetails shapes, auth (401/403), rate limiting (429), pagination contracts
- Add permission matrix tests (role x endpoint combinations)

### Frontend E2E: Incremental Playwright Overhaul

- Add Playwright **setup project** for global auth with `storageState` reuse
- Add `data-testid` attributes to critical interactive components
- Keep existing API-seeding helper functions for test data
- Defer full Page Object Model until test count exceeds ~30 specs
- Standardize on `test.extend` fixtures pattern (replace inconsistent helper functions)

## Alternatives Considered

### Backend
1. **Per-class containers (current pattern)** — Simple but slow. Each class spins up its own Postgres container. Does not scale.
2. **Shared container without Respawn** — Faster startup but risks test pollution without DB reset between tests.

### Frontend
1. **Full POM + globalSetup + data-testid** — Maximum structure but high upfront cost for 13 specs. Maintenance overhead of page objects exceeds value at current scale.
2. **Playwright fixtures only (test.extend)** — Good abstraction but doesn't solve auth sharing or selector reliability.

## Consequences

### Positive
- HTTP-level tests catch wire-format bugs (serialization, middleware, auth) that handler-only tests miss
- Shared container cuts CI test time by ~60% vs per-class containers
- Respawn ensures test isolation without container restart overhead
- `data-testid` eliminates flaky text-based selectors
- `storageState` reuse eliminates repeated login flows in E2E

### Negative
- Respawn adds a NuGet dependency and requires understanding of FK ordering
- `data-testid` attributes add minor maintenance burden to components
- WebApplicationFactory tests are slower than pure handler tests — use for integration, not unit testing
- Incremental Playwright approach means some structural debt until POM is adopted

### Risks
- Docker availability in CI (mitigated: Testcontainers has CI-detection built in)
- Respawn FK ordering issues on complex schemas (mitigated: Respawn v4+ handles this automatically)
- `data-testid` proliferation if not scoped to interactive elements (mitigated: only add to buttons, forms, navigation, data displays)

## Implementation Plan

### Phase 1: Backend Infrastructure (M)
1. Add Respawn NuGet package
2. Create `PostgresFixture` with `ICollectionFixture` pattern
3. Create `IntegrationTestWebAppFactory` extending `WebApplicationFactory<Program>`
4. Create base class `ApiIntegrationTestBase` with authenticated HttpClient helpers
5. Migrate one existing test file as proof of concept

### Phase 2: Backend Test Coverage (L)
1. Add HTTP-level tests for all Sprint endpoints (Phase 3)
2. Add permission matrix tests (role x endpoint)
3. Add rate limiting tests
4. Add health check degraded state tests

### Phase 3: Playwright Infrastructure (S)
1. Add setup project to `playwright.config.ts` for global auth
2. Add `data-testid` to critical components (sprint board, forms, navigation)
3. Standardize fixtures on `test.extend` pattern
4. Add `afterAll` cleanup hooks

### Phase 4: Playwright Coverage (M)
1. Add E2E tests for new sprint planning flows
2. Add visual regression baseline screenshots
3. Verify dark/light mode rendering
