# Discovery Context — Integration & E2E Testing with Playwright

## Requirements
Full coverage strategy: integration tests for all backend handlers (Application layer, API controllers, repositories, background jobs) + Playwright E2E tests for critical user flows. Goal is a comprehensive, long-term testing foundation for all future phases.

## Context
- Long-term strategy: building a testing foundation that scales across all future phases
- Not just Phase 3 hardening — this is an architectural decision for the project's testing approach
- Must work in CI pipeline

## Preferences
- Testcontainers + Playwright: real PostgreSQL for backend integration tests, real browser for E2E
- No mocks for integration tests — use real DB
- Playwright for all E2E/browser tests

## Current State (from Scout)
- 6 backend test projects exist, Application tests use NSubstitute mocks
- WebApplicationFactory installed but NOT used — no HTTP-level tests
- No repository integration tests beyond PermissionChecker
- Playwright is set up with 13 E2E specs but has inconsistent fixtures, no shared auth state, no data-testid
- Zero frontend component/unit tests
- Each test class starts its own Postgres container (slow at scale)
