## TFD Review — 260315

**Status**: COMPLETED
**Concern**: tfd
**Files reviewed**: 34 (22 unit, 4 integration, 2 domain, 2 infra-test, 9 builders/base, 4 source validators cross-checked)

---

### TFD Compliance

Git history unavailable for Phase 1 features. All feature test files and handler files are untracked (working tree only, never committed). No per-file commit timestamps exist to determine test-before-implementation order. TFD compliance for Phase 1 **cannot be verified from git history** and must be confirmed by the developer.

| Module | Test Added | Impl Added | Order | Verdict |
|--------|-----------|------------|-------|---------|
| All Phase 1 features | Untracked | Untracked | Unknown | UNVERIFIABLE — not committed |
| Phase 0 foundation (PagedResult, ValidationBehavior, EntityTests, EnumTests) | commit fb37940 | commit fb37940 | Same commit | UNVERIFIABLE |

---

### Coverage Assessment

| Area | Target | Estimated | Gap |
|------|--------|-----------|-----|
| WorkItem handlers (12 handlers) | 100% | ~90% | Permission-denied path missing in most handlers |
| Project handlers (6 handlers) | 100% | ~95% | ArchiveProject missing permission-denied test |
| Release handlers (6 handlers) | 100% | ~85% | GetRelease, ListReleases — zero unit tests |
| Backlog handlers (2 handlers) | 100% | ~80% | ReorderBacklog — only 1 test, no empty-list case |
| Kanban handler (1 handler) | 100% | ~85% | Empty board (no items) case not tested |
| Validators (CreateWorkItem, UpdateWorkItem, CreateRelease, CreateProject, UpdateProject) | 100% | ~80% | MaxLength boundary values untested across all validators |
| Integration tests | 20% target | ~18% | Kanban, Backlog, Assign/Unassign flows missing E2E tests |
| Domain entity/enum tests | 70% | ~60% | No behavior tests; all are builder-smoke and enum-count checks |
| Overall unit/integration/E2E ratio | 70U/20I/10E2E | ~75U/18I/7E2E | Integration slightly under; E2E not present (no HTTP-level tests) |

---

### Frontend TFD Compliance

Not applicable — no frontend files in scope.

---

### Test Quality Findings

| File | Line | Severity | Category | Title | Detail | Suggestion | Confidence |
|------|------|----------|----------|-------|--------|------------|------------|
| `CreateWorkItemTests.cs` | all | HIGH | Missing scenario | No permission-denied test | `Handle_ValidCommand` always stubs `HasPermissionAsync = true`; the `Forbidden` branch of every handler is never exercised in unit tests | Add `Handle_NoPermission_ReturnsForbidden` for each mutating handler using `_permissions.HasPermissionAsync(...).Returns(false)` | HIGH |
| `AssignItemTests.cs` | 83–86 | HIGH | Coverage theater — stub test | `ListReleases_ReturnsPaged` is an empty test body | Test method exists with a comment "Basic smoke test handled in ListReleasesHandler" but contains zero assertions and no arrange/act | Remove or implement; as-is it passes trivially and hides a coverage gap | HIGH |
| `DeleteReleaseTests.cs` | 56–60 | MEDIUM | Weak assertion | `Handle_NonExistentRelease_ReturnsNotFound` missing error string check | Only asserts `result.IsFailure` — does not verify the error message matches "Release not found" | Add `result.Error.Should().Be("Release not found")` to match sibling test style | MEDIUM |
| `UnassignWorkItemTests.cs` | 59–67 | MEDIUM | Weak assertion | `Handle_NonExistentItem_ReturnsNotFound` has no error-text assertion | Asserts only `result.IsFailure.Should().BeTrue()` | Add `result.Error.Should().Contain("not found")` | MEDIUM |
| `MoveWorkItemTests.cs` | 49–63 | MEDIUM | Weak assertion | `Handle_InvalidReparent_ReturnsError` asserts only `result.IsFailure` | Does not verify the error message; similar tests across the suite do verify message content | Add `result.Error.Should().Contain(...)` with expected text | MEDIUM |
| `ValidationBehaviorTests.cs` | 25–38 | LOW | Convention violation | Uses `Assert.True/Assert.False` instead of FluentAssertions | CLAUDE.md mandates FluentAssertions; three tests use xUnit Assert directly (`Assert.True(nextCalled)`, etc.) | Replace with `nextCalled.Should().BeTrue()` and `result.IsSuccess.Should().BeTrue()` | HIGH |
| `EnumTests.cs` | 48–51 | LOW | Weak assertion | `Priority_ShouldHaveFourValues` does not name the four values | Only checks count; if an enum member is renamed the test still passes | Add `Assert.Contains(Priority.Low, values)` etc. for all members | LOW |
| `EntityTests.cs` | 27–35 | LOW | Coverage theater | `WorkItem_SoftDelete_ShouldSetDeletedAt` manually sets `DeletedAt` via property assignment | This tests C# property assignment, not domain behavior; there is no domain method being exercised | Remove or replace with a test that exercises a real soft-delete path | LOW |
| All mutating handlers | — | HIGH | Missing scenario | Missing `Forbidden` (403) path in all unit tests | `ArchiveProjectTests`, `DeleteProjectTests`, `UpdateProjectTests`, `ChangeStatusTests`, `MoveWorkItemTests`, `AssignWorkItemTests`, `UnassignWorkItemTests`, `DeleteWorkItemTests`, `UpdateWorkItemTests`, `AddLinkTests`, `RemoveLinkTests` — none test the `HasPermissionAsync = false` branch | Add one `[Fact] Handle_NoPermission_ReturnsForbidden` per handler; use `_permissions.HasPermissionAsync(...).Returns(false)` | HIGH |
| `ReorderBacklogTests.cs` | all | MEDIUM | Missing scenario | Empty item list not tested | Only happy path with 2 items; an empty `items` array is a valid input and `UpdateSortOrderAsync` should be called 0 times | Add `Handle_EmptyItems_NoUpdateCalled` | MEDIUM |
| `GetBacklogTests.cs` / `GetKanbanBoardTests.cs` | all | MEDIUM | Missing scenario | Empty result set not tested | Neither handler test verifies behavior when repository returns 0 items | Add tests for empty collections | MEDIUM |
| `AssignItemTests.cs` | all | MEDIUM | Missing scenario | Assigning to a released release not tested | `AssignItemToReleaseHandler` may guard against assigning to a `Released` status release; no unit test covers this | Add `Assign_ReleasedRelease_ReturnsError` | MEDIUM |
| `CreateWorkItemTests.cs` | all | LOW | Missing scenario | Title at MaxLength boundary (500 chars) not tested in validator | `Validate_EmptyTitle` covers empty/null; 500-char and 501-char boundaries are not tested | Add `[InlineData(500, true)]` / `[InlineData(501, false)]` via `[Theory]` | LOW |

---

### Summary

The test suite has solid happy-path and not-found coverage for all 20+ handlers, good use of builders throughout, and meaningful integration tests covering the three key flows (project lifecycle, hierarchy cascade, item linking). The most significant gap is systematic: the permission-denied (`Forbidden`) branch is never exercised in any unit test — every test class stubs `HasPermissionAsync = true` in the constructor with no test reversing it. One test method (`ListReleases_ReturnsPaged` in `AssignItemTests`) is a stub with an empty body and zero assertions, constituting coverage theater.

### Unresolved Questions

- Were Phase 1 feature tests written before their paired handlers? This cannot be confirmed from git history because all Phase 1 files remain uncommitted. Developer attestation or a pre-commit ordering audit is required to close this TFD compliance gap.
- `GetRelease` and `ListReleases` handlers exist in source but have zero dedicated unit test files — confirm these are intentionally deferred or add tests.
