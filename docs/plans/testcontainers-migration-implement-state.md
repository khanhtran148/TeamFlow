# Implementation State — Testcontainers Migration

## Topic
Migrate all backend tests from NSubstitute mocks and SQLite in-memory to Testcontainers with real PostgreSQL

## Discovery Context
- **Branch:** feat/user-profile (continue on current)
- **Requirements:** Full migration per plan — Application.Tests, BackgroundServices.Tests, Hub test stays
- **Test DB Strategy:** Testcontainers with real PostgreSQL
- **Feature Scope:** Backend-only
- **Task Type:** refactor

## Phase-Specific Context
- **Plan dir:** docs/plans/testcontainers-migration
- **Plan source:** docs/plans/testcontainers-migration/plan.md
- **User modifications:** None

### Plan Summary
8 phases, ~138 files:
- Phase 0: 9 files stay as-is (pure logic, no mocks)
- Phase 1: Infrastructure — PostgresCollectionFixture, ApplicationTestBase, CapturingPublisher, AlwaysDenyTestPermissionChecker, register all 27 repos
- Phases 2-5: Batch migrate 124 Application.Tests files by domain (WorkItems, Projects, Sprints, Social+Auth)
- Phase 6: Migrate BackgroundServices.Tests from SQLite to Testcontainers
- Phase 7: Hub test stays mocked
- Phase 8: Cleanup — remove unused packages, verify suite

### Key Decisions
- Use ISender.Send() through MediatR instead of direct handler construction
- 5 parallel xUnit collection fixtures (5 containers) for throughput
- Transaction rollback per test for isolation
- Keep NSubstitute for non-DB services (IPublisher, IBroadcastService, IAuthService)
