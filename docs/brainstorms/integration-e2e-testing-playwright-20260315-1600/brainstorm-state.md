# Brainstorm State — Integration & E2E Testing Strategy

## Topic
How to cover logic/app layer with integration tests and E2E tests using Playwright

## Mode
Default (full workflow)

## Discovery Answers
- **Requirements**: Full coverage strategy — integration tests for all backend layers + Playwright E2E for critical user flows
- **Context**: Long-term testing foundation for all future phases, must work in CI
- **Preferences**: Testcontainers + Playwright (real DB for integration, real browser for E2E)

## Current State
- 6 backend test projects, Application tests use NSubstitute mocks
- WebApplicationFactory installed but NOT used — no HTTP-level tests
- No repository integration tests beyond PermissionChecker
- Playwright set up with 13 E2E specs but inconsistent fixtures, no shared auth, no data-testid
- Zero frontend component tests
- Each test class starts own Postgres container (slow)

## Raw Ideas (24 total)
1. WebApplicationFactory integration test layer
2. Playwright test architecture overhaul (POM, globalSetup, data-testid, cleanup)
3. Test pyramid with shared container fixture (ICollectionFixture)
4. Contract testing with Verify/Snapshots for API shapes
5. Permission matrix integration tests (role × endpoint)
6. Rate limiting integration tests (429 + Retry-After)
7. SignalR integration tests (HubConnection client)
8. Health check integration tests (kill containers, verify Degraded)
9. Repository integration tests with Testcontainers
10. Migration smoke tests (apply from zero)
11. Seed data validation tests
12. Performance regression tests (1000-item benchmark)
13. Job execution integration tests (real DB side effects)
14. MassTransit consumer integration tests (InMemoryTestHarness)
15. Quartz schedule registration tests
16. Page Object Model for Playwright
17. Visual regression testing (screenshot comparison)
18. API mocking in E2E (Playwright route())
19. Accessibility testing (axe-core)
20. Cross-browser matrix (Chromium + Firefox + WebKit)
21. Playwright component testing (experimental CT)
22. CI test orchestration (parallel stages, early exit)
23. Test data factory service (centralized scenario creation)
24. Flaky test detection (re-run + quarantine)

## Confirmed Clusters for Deep Research

### Cluster A: WebApplicationFactory Integration Layer (Score: 13/15)
Ideas: 1, 4, 5, 6, 8
- Add HTTP-level integration tests using WebApplicationFactory<Program> + shared Testcontainers
- Verify status codes, ProblemDetails shapes, auth/403 responses, pagination contracts
- Permission matrix tests (every role × endpoint)
- Rate limiting verification
- Health check degraded state tests

### Cluster D: Playwright Architecture Overhaul (Score: 12/15)
Ideas: 2, 16, 17, 23
- Restructure E2E with globalSetup for auth (storageState reuse)
- Page Object Model for reusable page interactions
- Consistent data-testid attributes across all components
- Proper cleanup hooks (afterAll)
- Test data factory for centralized scenario creation
- Visual regression with screenshot comparison

## User Research Instructions
None specified — use defaults.

## Project Context
- .NET 10, Next.js App Router, PostgreSQL, RabbitMQ, SignalR
- CLAUDE.md conventions: TFD, sealed classes, Result pattern, CQRS
- Test stack: xUnit + FluentAssertions + NSubstitute + Testcontainers + Playwright
