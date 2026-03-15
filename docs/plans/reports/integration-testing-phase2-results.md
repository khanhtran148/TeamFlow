# Integration Testing Phase 2 -- Backend Coverage Results

**Date:** 2026-03-15
**Status:** COMPLETED
**Branch:** `feat/phase-3-sprint-hardening`
**API Contract:** `docs/plans/integration-e2e-testing/api-contract-260315-1800.md` (v1.0.0, no breaking changes)

---

## Summary

All 104 new integration tests pass. Zero regressions introduced.
8 pre-existing test failures (Releases, WorkItems, Projects) were already broken before this phase.

## Completed Endpoints + Test Coverage

| Task | File | Tests | Status |
|------|------|-------|--------|
| 2.1 Sprint CRUD | `SprintCrudTests.cs` | 13 | PASS |
| 2.2 Sprint Lifecycle | `SprintLifecycleTests.cs` | 12 | PASS |
| 2.3 Permission Matrix | `SprintPermissionMatrixTests.cs` | 66 | PASS |
| 2.4 Rate Limiting | `RateLimitIntegrationTests.cs` | 3 | PASS |
| 2.5 Health Checks | `HealthCheckTests.cs` | 4 | PASS |
| 2.6 ProblemDetails Shape | `ProblemDetailsShapeTests.cs` | 5 | PASS |
| **TOTAL** | **6 files** | **103** | **ALL PASS** |

(+1 test from pre-existing ProjectHttpTests.HealthCheck_Returns200 that also runs under the filter)

## TFD Compliance

| Layer | Approach |
|-------|----------|
| Sprint CRUD tests | Tests written first, then helpers added to make them pass |
| Sprint Lifecycle tests | Tests written first, discovered role/permission issues during RED phase |
| Permission Matrix | Theory + MemberData written first, seeding helpers refined during GREEN |
| Rate Limiting | Custom WebAppFactory with low limits designed before tests |
| Health Checks | Tests driven by expected JSON shape |
| ProblemDetails Shape | Tests driven by RFC 7807 requirements |

## Mocking Strategy

- **Database:** Real PostgreSQL via Testcontainers (single shared container per collection)
- **DB Reset:** Respawn between each test class
- **Auth:** Real JWT generation with test secret; real auth middleware pipeline
- **Broadcasting:** `NullBroadcastService` (no-op SignalR)
- **RabbitMQ:** `AlwaysHealthyCheck` (no-op health check)
- **Rate Limiting:** Custom `RateLimitTestWebAppFactory` with `WritePermitLimit=3` for fast 429 testing
- **No Docker for application:** Only Testcontainers for PostgreSQL

## Key Findings / Deviations

### Permission Bootstrap Fix (ApiIntegrationTestBase)
The `PermissionChecker` has bootstrap logic that allows all operations when no OrgAdmin exists for an org. This caused permission-denied tests to pass incorrectly (returning 201 instead of 403). Fixed by seeding an OrgAdmin "anchor" user (`SeedUser2Id`) whenever testing a non-OrgAdmin role.

### JSON Deserialization
The API serializes enums as strings (`JsonStringEnumConverter`) but `ReadFromJsonAsync<T>()` uses default options expecting integer enums. Created `TestJsonOptions.Default` with `JsonStringEnumConverter` to match.

### Permission Matrix Corrections from Plan
The plan's matrix listed ProductOwner and TechLead as having Sprint Start/Complete access. The actual `PermissionMatrix.cs` does NOT grant `Sprint_Start` or `Sprint_Complete` to ProductOwner or TechnicalLeader. Only OrgAdmin and TeamManager have these permissions. Tests match the actual implementation -- the api-contract document was updated accordingly.

## Files Created / Modified

### Created (Phase 2 owned)
- `tests/TeamFlow.Api.Tests/Sprints/SprintCrudTests.cs`
- `tests/TeamFlow.Api.Tests/Sprints/SprintLifecycleTests.cs`
- `tests/TeamFlow.Api.Tests/Sprints/SprintPermissionMatrixTests.cs`
- `tests/TeamFlow.Api.Tests/RateLimiting/RateLimitIntegrationTests.cs`
- `tests/TeamFlow.Api.Tests/RateLimiting/RateLimitTestBase.cs`
- `tests/TeamFlow.Api.Tests/RateLimiting/RateLimitTestWebAppFactory.cs`
- `tests/TeamFlow.Api.Tests/Health/HealthCheckTests.cs`
- `tests/TeamFlow.Api.Tests/ErrorHandling/ProblemDetailsShapeTests.cs`
- `tests/TeamFlow.Api.Tests/Infrastructure/TestJsonOptions.cs`
- `docs/plans/integration-e2e-testing/api-contract-260315-1800.md`

### Modified (Phase 1 owned, minor enhancement)
- `tests/TeamFlow.Api.Tests/Infrastructure/ApiIntegrationTestBase.cs` -- Added OrgAdmin anchor seeding and User2 creation in `SeedProjectAsync`

## Unresolved Questions / Blockers

None. All 6 tasks completed successfully.

## Pre-existing Failures (NOT introduced by this phase)

8 tests in existing classes fail (all were broken before this phase):
- `ReleaseAssignmentTests.ReleaseLifecycle_CreateAssignVerifyDeleteUnlinks`
- `WorkItemHierarchyTests.HierarchyLifecycle_CreateEpicStoryTask_DeleteEpicCascades`
- `WorkItemHierarchyTests.MoveWorkItem_StoryBetweenEpics_Succeeds`
- `ItemLinkingTests.*` (4 tests)
- `ProjectLifecycleTests.ProjectLifecycle_CreateUpdateArchiveListDelete`
