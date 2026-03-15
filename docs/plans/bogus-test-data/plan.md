# Plan: Integrate Bogus into Test Builders

**Date:** 2026-03-16
**Scope:** Backend only (TeamFlow.Tests.Common)
**Branch:** `feature/bogus-test-data`

---

## Phase Status

| Phase | Name | Status |
|-------|------|--------|
| 1 | Package Setup and Faker Infrastructure | completed |
| 2 | Enhance Builders with TFD | completed |
| 3 | Full Regression Run | completed |

### Success Criteria
- [x] Bogus 35.6.5 added to TeamFlow.Tests.Common.csproj
- [x] FakerProvider static class created at tests/TeamFlow.Tests.Common/Fakers/FakerProvider.cs
- [x] 7 builders updated with Faker-generated defaults (UserBuilder, OrganizationBuilder, ProjectBuilder, WorkItemBuilder, SprintBuilder, ReleaseBuilder, TeamBuilder)
- [x] 16 BuilderFakerTests pass in TeamFlow.Domain.Tests/Builders/BuilderFakerTests.cs
- [x] Full regression: 513 tests pass, 0 failures across all 6 test projects

---

## Goal

Replace hardcoded string defaults in all 11 test data builders with Bogus-generated fake data. Keep the fluent builder API unchanged. No breaking changes to existing tests.

---

## Phase 1: Package Setup and Faker Infrastructure

**Tasks:**

1. **Add Bogus NuGet package** to `TeamFlow.Tests.Common.csproj`.
   - `dotnet add tests/TeamFlow.Tests.Common package Bogus`
   - FILE: `tests/TeamFlow.Tests.Common/TeamFlow.Tests.Common.csproj`

2. **Create `FakerProvider` static helper class** that exposes a shared `Faker` instance.
   - Location: `tests/TeamFlow.Tests.Common/Fakers/FakerProvider.cs`
   - Sealed static class with a single `public static Faker Instance { get; }` property
   - No global seed -- each `New()` call gets fresh random data, which is the desired behavior for independent test isolation
   - If a test needs deterministic data, it uses the existing `.With*()` overrides

```csharp
// tests/TeamFlow.Tests.Common/Fakers/FakerProvider.cs
using Bogus;

namespace TeamFlow.Tests.Common.Fakers;

public static class FakerProvider
{
    private static readonly Lock Lock = new();

    public static Faker Instance
    {
        get
        {
            lock (Lock)
            {
                return new Faker();
            }
        }
    }
}
```

**Why a shared accessor instead of inline `new Faker()` in each builder?** Single import, one place to change locale or add customization later. The lock + new instance per access ensures thread safety without shared mutable state.

**FILE OWNERSHIP:** `FakerProvider.cs`, `TeamFlow.Tests.Common.csproj`

---

## Phase 2: Enhance Builders (TFD per builder)

For each builder below, the workflow is:

1. Write a test that calls `.New().Build()` twice and asserts the two instances have different generated string values (proving Faker randomization works)
2. Update the builder's field defaults to use `FakerProvider.Instance`
3. Run all existing tests to confirm no regressions

**Test file:** `tests/TeamFlow.Tests.Common.Tests/Builders/BuilderFakerTests.cs` (new test project or add to an existing test project that references Tests.Common)

Since `TeamFlow.Tests.Common` is not a test project itself (`IsTestProject=false`), builder faker tests go into a new file in `TeamFlow.Domain.Tests` (which already references Tests.Common):
`tests/TeamFlow.Domain.Tests/Builders/BuilderFakerTests.cs`

### Builder-by-Builder Field Mapping

Each entry lists: field name, current default, Faker replacement.

#### 1. UserBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_email` | `"test@teamflow.dev"` | `f.Internet.Email()` |
| `_name` | `"Test User"` | `f.Name.FullName()` |
| `_passwordHash` | `"hashed_Test@1234"` | **Keep as-is** (not real data, just a marker) |

FILE: `tests/TeamFlow.Tests.Common/Builders/UserBuilder.cs`

#### 2. OrganizationBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_name` | `"Test Organization"` | `f.Company.CompanyName()` |

FILE: `tests/TeamFlow.Tests.Common/Builders/OrganizationBuilder.cs`

#### 3. ProjectBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_name` | `"Test Project"` | `f.Commerce.ProductName()` |
| `_description` | `null` | **Keep null** (optional field, callers use `.WithDescription()`) |
| `_status` | `"Active"` | **Keep as-is** (enum-like string, not suitable for Faker) |
| `_orgId` | `Guid.NewGuid()` | **Keep as-is** |

FILE: `tests/TeamFlow.Tests.Common/Builders/ProjectBuilder.cs`

#### 4. WorkItemBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_title` | `"Test Work Item"` | `f.Lorem.Sentence(4)` |
| `_description` | `null` | **Keep null** |
| `_type` | `WorkItemType.UserStory` | **Keep as-is** (enum) |
| `_status` | `WorkItemStatus.ToDo` | **Keep as-is** (enum) |
| `_priority` | `Priority.Medium` | **Keep as-is** (enum) |
| All Guid fields | `Guid.NewGuid()` / `null` | **Keep as-is** |
| `_estimationValue` | `null` | **Keep null** |

FILE: `tests/TeamFlow.Tests.Common/Builders/WorkItemBuilder.cs`

#### 5. SprintBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_name` | `"Test Sprint"` | `$"Sprint {f.Random.Number(1, 99)}"` |
| `_goal` | `null` | **Keep null** |
| `_startDate` / `_endDate` | `null` | **Keep null** |
| `_status` | `SprintStatus.Planning` | **Keep as-is** |
| `_projectId` | `Guid.NewGuid()` | **Keep as-is** |

FILE: `tests/TeamFlow.Tests.Common/Builders/SprintBuilder.cs`

#### 6. ProjectMembershipBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_memberType` | `"User"` | **Keep as-is** (domain constant) |
| `_role` | `ProjectRole.Developer` | **Keep as-is** (enum) |
| All Guid fields | `Guid.NewGuid()` | **Keep as-is** |

**No Faker changes needed.** All fields are Guids or enums. Skip this builder.

FILE: No changes.

#### 7. ReleaseBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_name` | `"v1.0.0"` | `$"v{f.Random.Number(1, 9)}.{f.Random.Number(0, 20)}.{f.Random.Number(0, 99)}"` |
| `_description` | `null` | **Keep null** |
| `_releaseDate` | `null` | **Keep null** |
| `_status` | `ReleaseStatus.Unreleased` | **Keep as-is** |
| `_notesLocked` | `false` | **Keep as-is** |

FILE: `tests/TeamFlow.Tests.Common/Builders/ReleaseBuilder.cs`

#### 8. WorkItemHistoryBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_actorType` | `"User"` | **Keep as-is** (domain constant) |
| `_actionType` | `"Created"` | **Keep as-is** (domain constant) |
| `_fieldName` / `_oldValue` / `_newValue` | `null` | **Keep null** |
| All Guid fields | `Guid.NewGuid()` / `null` | **Keep as-is** |

**No Faker changes needed.** All meaningful strings are domain constants. Skip this builder.

FILE: No changes.

#### 9. WorkItemLinkBuilder

**No Faker changes needed.** All fields are Guids or enums. Skip this builder.

FILE: No changes.

#### 10. TeamBuilder
| Field | Current | Faker |
|-------|---------|-------|
| `_name` | `"Test Team"` | `f.Commerce.Department()` |
| `_description` | `null` | **Keep null** |
| `_orgId` | `Guid.NewGuid()` | **Keep as-is** |

FILE: `tests/TeamFlow.Tests.Common/Builders/TeamBuilder.cs`

#### 11. BurndownDataPointBuilder

**No Faker changes needed.** All fields are Guids, ints, bools, or dates. Skip this builder.

FILE: No changes.

### Summary: Builders That Change

| Builder | Fields Getting Faker | Count |
|---------|---------------------|-------|
| UserBuilder | `_email`, `_name` | 2 |
| OrganizationBuilder | `_name` | 1 |
| ProjectBuilder | `_name` | 1 |
| WorkItemBuilder | `_title` | 1 |
| SprintBuilder | `_name` | 1 |
| ReleaseBuilder | `_name` | 1 |
| TeamBuilder | `_name` | 1 |

**7 builders change. 4 builders stay unchanged** (ProjectMembership, WorkItemHistory, WorkItemLink, BurndownDataPoint).

---

## Phase 3: Full Regression Run

**Tasks:**

1. Run `dotnet test` across all 6 test projects
2. Fix any tests that assert against the old hardcoded defaults
3. Verify no test depends on two independently-built objects having identical default values

### Known Hardcoded Default Usage (found via grep)

These files use hardcoded strings that match builder defaults. Most are safe because they explicitly pass values via `.With*()` or construct commands directly (not relying on builder defaults). Review each during regression:

- `tests/TeamFlow.Api.Tests/Sprints/SprintLifecycleTests.cs:244` -- `.WithTitle("Test Work Item")` -- **Safe**, explicit override
- `tests/TeamFlow.Api.Tests/Sprints/SprintCrudTests.cs:251` -- `.WithTitle("Test Work Item")` -- **Safe**, explicit override
- `tests/TeamFlow.Api.Tests/Sprints/SprintPermissionMatrixTests.cs:377,450` -- `.WithName("Test Sprint")`, `.WithTitle("Test Work Item")` -- **Safe**, explicit overrides
- `tests/TeamFlow.Api.Tests/WorkItems/WorkItemHierarchyTests.cs:27` -- `"Test Project"` in command -- **Safe**, explicit string
- `tests/TeamFlow.Api.Tests/WorkItems/ItemLinkingTests.cs:94` -- `"Test Project"` in command -- **Safe**, explicit string
- `tests/TeamFlow.Api.Tests/Infrastructure/ApiIntegrationTestBase.cs:84,107` -- hardcoded `"test@teamflow.dev"`, `"Test User"`, `"Test Project"` -- **Safe**, these are test infrastructure constants, not builder defaults
- `tests/TeamFlow.Tests.Common/TestDataSeeder.cs:24` -- hardcoded user creation -- **Safe**, does not use builder
- `tests/TeamFlow.Tests.Common/TestStubs.cs:9-10` -- `StubCurrentUser` with hardcoded values -- **Safe**, independent of builder
- `tests/TeamFlow.Application.Tests/Features/Releases/GetReleaseTests.cs:28` -- asserts `"v1.0.0"` but uses `.WithName("v1.0.0")` on line 20 -- **Safe**, explicit override
- `tests/TeamFlow.Application.Tests/Features/Auth/*` -- hardcoded strings in command construction -- **Safe**, not using builders

**Conclusion:** No existing tests rely on implicit builder defaults for assertions. All use explicit `.With*()` overrides or construct values directly. Regression risk is minimal.

**FILE OWNERSHIP:** Any test files that assert hardcoded builder defaults

---

## Risk Considerations

### Test Determinism
- Bogus without a global seed means each run produces different data. This is intentional and desirable -- tests should not depend on specific string values.
- If a test fails intermittently after this change, it reveals a hidden dependency on hardcoded values, which is a test smell worth fixing.
- Tests that need specific values already call `.WithName("specific")` etc., so they are unaffected.

### Thread Safety
- `FakerProvider` creates a new `Faker` instance per access inside a lock. No shared mutable state.
- Parallel test execution (xUnit default) is safe.

### Breaking Change Risk
- **Low.** The `.With*()` methods are unchanged. Only the defaults from `.New()` change.
- **Potential issue:** Tests that build two objects via `.New().Build()` and then compare their names for equality. These would fail because names are now random. Such tests would be incorrect anyway (testing implementation detail of the builder, not domain behavior).

### Bogus Version
- Use latest stable (v35.x as of 2026). No known compatibility issues with .NET 10.

---

## Implementation Order

```
Phase 1 (10 min)  : Package + FakerProvider
Phase 2 (30 min)  : 7 builder updates with TFD tests
Phase 3 (10 min)  : Full regression run + fixups
```

Total estimated time: ~50 minutes.

---

## Approval

Approve this plan to proceed with `/mk-implement`.
