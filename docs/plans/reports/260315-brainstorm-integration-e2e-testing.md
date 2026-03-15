# Brainstorm Report: Integration & E2E Testing Strategy

**Date:** 2026-03-15
**Topic:** How to cover logic/app layer with integration tests and E2E tests using Playwright
**ADR:** [260315-integration-e2e-testing-strategy.md](../../adrs/260315-integration-e2e-testing-strategy.md)

## Summary

Brainstormed 24 ideas across 8 clusters. Deep-researched the top 2 clusters (WebApplicationFactory Integration, Playwright Architecture Overhaul). Scored 6 options across Speed, Maintainability, CI Friendliness, Migration Effort, and Test Reliability.

## Decision

**Backend:** WebApplicationFactory + ICollectionFixture + Respawn (Score: 39/50)
**Frontend:** Incremental Playwright overhaul — setup project + data-testid first, POM later (Score: 40/50)

## Key Findings

1. **WebApplicationFactory closes the biggest gap**: Handler-only tests miss wire-format issues (serialization, middleware, auth headers, rate limiting). WAF tests catch these at the HTTP level.

2. **Shared containers + Respawn = fast CI**: ICollectionFixture shares one Postgres container per collection. Respawn resets data between tests in ~50ms vs ~5s for container restart.

3. **Incremental Playwright wins over full POM**: At 13 specs, POM maintenance overhead exceeds value. Setup project + data-testid captures 80% of the reliability gain at 40% of the cost.

4. **data-testid scoped to interactive elements**: Only buttons, forms, navigation, and data displays. Avoid adding to every div/span.

## Rejected Alternatives

| Alternative | Reason |
|-------------|--------|
| Per-class containers | Does not scale — each class spins up its own Postgres |
| Full POM immediately | Overkill for 13 specs, deferred until >30 |
| Mocks-only integration | Misses wire-format bugs, doesn't match "real DB" preference |
| Playwright CT (component testing) | Experimental, limited React Server Component support |

## Next Steps

See ADR implementation plan (4 phases). Estimated effort: M-L total.
Can be implemented as part of Phase 3.4 hardening or as a standalone initiative.
