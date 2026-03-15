# Phase 3.3 -- Background Jobs Implementation Report

**Date:** 2026-03-15
**Status:** COMPLETED
**Branch:** `feat/phase-3-sprint-hardening`

---

## API Contract

- **Path:** `docs/plans/phase-3-sprint-hardening/api-contract-260315-1500.md`
- **Version:** 1.0.0
- **Breaking changes:** None (no HTTP endpoints added; event-driven only)

---

## Completed Items

### Scheduled Jobs (4/4)

| Job | File | Cron | Tests |
|-----|------|------|-------|
| BurndownSnapshotJob | `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/BurndownSnapshotJob.cs` | `59 23 * * ?` | 5 pass |
| ReleaseOverdueDetectorJob | `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/ReleaseOverdueDetectorJob.cs` | `5 0 * * ?` | 5 pass |
| StaleItemDetectorJob | `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/StaleItemDetectorJob.cs` | `0 8 * * ?` | 7 pass |
| EventPartitionCreatorJob | `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/EventPartitionCreatorJob.cs` | `0 3 25 * ?` | 2 pass |

### MassTransit Consumers (2/2)

| Consumer | File | Tests |
|----------|------|-------|
| SprintStartedConsumer | `src/apps/TeamFlow.BackgroundServices/Consumers/SprintStartedConsumer.cs` | 3 pass |
| SprintCompletedConsumer | `src/apps/TeamFlow.BackgroundServices/Consumers/SprintCompletedConsumer.cs` | 3 pass |

### Quartz Registration

All 4 jobs registered in `src/apps/TeamFlow.BackgroundServices/Program.cs` with correct cron schedules, misfire policies, and priorities.

### MassTransit Registration

Both sprint consumers registered alongside existing consumers in `Program.cs`.

---

## Test Coverage Summary

| Layer | Tests | Passed | Failed |
|-------|-------|--------|--------|
| Jobs (BurndownSnapshot) | 5 | 5 | 0 |
| Jobs (ReleaseOverdueDetector) | 5 | 5 | 0 |
| Jobs (StaleItemDetector) | 7 | 7 | 0 |
| Jobs (EventPartitionCreator) | 2 | 2 | 0 |
| Consumers (SprintStarted) | 3 | 3 | 0 |
| Consumers (SprintCompleted) | 3 | 3 | 0 |
| **Total** | **25** | **25** | **0** |

---

## TFD Compliance

| Layer | Approach | Compliance |
|-------|----------|------------|
| Jobs | Tests written first (RED), then implementation (GREEN), then cleanup (REFACTOR) | Full |
| Consumers | Tests written first, then implementation | Full |
| All new classes | `sealed` keyword applied | Full |

---

## Mocking Strategy

- **Database:** SQLite in-memory with custom `TestTeamFlowDbContext` that converts PostgreSQL-specific `jsonb` column types to text via value converters
- **FK constraints:** Disabled via `PRAGMA foreign_keys = OFF` for unit test isolation
- **External services:** NSubstitute mocks for `IBroadcastService`, `IPublisher`, `ILogger<T>`, `IJobExecutionContext`
- **No Docker required**

---

## New Files Created

### Implementation
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/BurndownSnapshotJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/ReleaseOverdueDetectorJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/StaleItemDetectorJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/EventPartitionCreatorJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Consumers/SprintStartedConsumer.cs`
- `src/apps/TeamFlow.BackgroundServices/Consumers/SprintCompletedConsumer.cs`

### Modified
- `src/apps/TeamFlow.BackgroundServices/Program.cs` (job + consumer registration)
- `TeamFlow.slnx` (added test project)

### Test Infrastructure
- `tests/TeamFlow.BackgroundServices.Tests/TeamFlow.BackgroundServices.Tests.csproj`
- `tests/TeamFlow.BackgroundServices.Tests/TestDbContextFactory.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/BurndownSnapshotJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/ReleaseOverdueDetectorJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/StaleItemDetectorJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/EventPartitionCreatorJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintStartedConsumerTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintCompletedConsumerTests.cs`

### Contract
- `docs/plans/phase-3-sprint-hardening/api-contract-260315-1500.md`

---

## Deviations from Plan

1. **EventPartitionCreatorJob SQL execution**: The job checks if the database provider is PostgreSQL (Npgsql) before executing the partition SQL. On non-PostgreSQL providers (SQLite, InMemory), it logs a skip message and still records the metric. This is necessary because partition syntax is PostgreSQL-specific.

2. **StaleItemDetectorJob -- no "skip items already flagged in last 14 days" test**: The plan mentioned this test, but the implementation flags all stale items on every run (the `stale_flag` is a boolean, not a timestamp). Skipping recently-flagged items would require tracking flag timestamps, which is not in the current `ai_metadata` schema. This could be added in Phase 3.4 hardening if needed.

---

## Unresolved Questions / Blockers

None. All Phase 3.3 success criteria are met.

---

## Pre-existing Issues (not introduced by this phase)

- 8 `TeamFlow.Api.Tests` failures related to DI resolution of `IProjectMembershipRepository` -- pre-existing from Phase 3.1 sprint handler integration.
