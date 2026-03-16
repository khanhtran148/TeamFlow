# Discovery Context — Testcontainers Migration

**Date:** 2026-03-16
**Branch:** feat/user-profile (continue on current branch)

## Requirements
Migrate all backend tests from NSubstitute mocks and SQLite in-memory to Testcontainers with real PostgreSQL.
- Application.Tests (133 files) — NSubstitute mocks → Testcontainers
- BackgroundServices.Tests (7 files) — SQLite in-memory → Testcontainers
- Api.Tests Hub test (1 file) — stays mocked (no DB benefit)

## Scope
Backend-only (test infrastructure migration)

## Test DB Strategy
Testcontainers with real PostgreSQL (the entire point of this migration)

## Success Criteria
All tests pass with real PostgreSQL via Testcontainers, zero NSubstitute for repositories.

## Additional Requirements
None — plan is complete as-is.
