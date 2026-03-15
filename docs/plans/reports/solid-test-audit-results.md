## Quality Review -- 260315

**Status**: COMPLETED
**Concern**: quality
**Files reviewed**: 45

### Findings
| File | Line | Severity | Category | Title | Detail | Suggestion | Confidence |
|------|------|----------|----------|-------|--------|------------|------------|
| TestJwtSettings.cs | 8 | medium | sealed-class | Static class but CLAUDE.md requires sealed | `TestJwtSettings` is `public static` -- cannot be sealed, but the CLAUDE.md convention says "all new classes MUST be sealed by default". Static classes are implicitly sealed in C#, so this is acceptable but inconsistent with explicit `sealed` keyword pattern. | No action needed; static is implicitly sealed. | 0.3 |
| EntityTests.cs | 62-73 | medium | DRY / builder-bypass | Raw `new WorkItemHistory{}` instead of builder | `WorkItemHistory_ShouldBeReadonly` test constructs entity inline despite `WorkItemHistoryBuilder` existing in Tests.Common. | Use `WorkItemHistoryBuilder.New().WithAction("StatusChanged").WithField("Status","ToDo","InProgress").Build()` | 0.9 |
| PermissionCheckerTests.cs | 44-126 | high | SRP / DRY | 85-line inline seeding method violates SRP | `SeedTestData()` is 85 lines of raw `new Entity{}` construction embedded in the test class. Uses raw `new User{}`, `new Project{}`, `new ProjectMembership{}`, `new Team{}`, `new TeamMember{}` instead of builders. | Extract to a dedicated `PermissionTestSeedHelper` in Tests.Common using existing builders. | 0.9 |
| PermissionCheckerTests.cs | 34-39 | medium | code-smell | Manual lazy-seed pattern with `_seeded` bool | `EnsureSeededAsync()` with a `_seeded` flag is a hand-rolled lazy init; fragile if test ordering changes. | Use xUnit `IAsyncLifetime` or a class fixture for one-time seed. | 0.8 |
| PermissionCheckerTests.cs | 15 | medium | DI-violation | Direct concrete `new PermissionChecker(DbContext)` | `Checker` property instantiates concrete `PermissionChecker` on every access instead of resolving via DI. | Register `PermissionChecker` in `ConfigureServices` and resolve from `Services`. | 0.7 |
| PermissionCheckerTests.cs | 29-32 | low | dead-code | Empty `ConfigureServices` override | Override returns `Task.CompletedTask` with no additions; serves no purpose. | Remove the override entirely. | 0.9 |
| IntegrationTestBase.cs | 82-83 | medium | DRY / builder-bypass | Raw `new Organization{}` with reflection ID set | Seed method uses `new Organization { Name = "Test Org" }` + `Entry().Property().CurrentValue` instead of `OrganizationBuilder`. | Use `OrganizationBuilder.New().WithName("Test Org").Build()` and add a `WithId()` method to builder. | 0.7 |
| IntegrationTestBase.cs | 89 | medium | DRY / builder-bypass | Raw `new User{}` with reflection ID set | Same pattern as above for seeded user. | Use `UserBuilder.New().Build()` and add `WithId()` to builder. | 0.7 |
| ApiIntegrationTestBase.cs | 77-96 | medium | DRY | Duplicated seed logic from IntegrationTestBase | `SeedReferenceDataAsync` in ApiIntegrationTestBase duplicates the same seeding code from `IntegrationTestBase.SeedReferenceDataAsync`. | Extract shared seed helper to Tests.Common. | 0.85 |
| IntegrationTestWebAppFactory.cs | 50-75 | medium | DRY | Third copy of seed reference data | `SeedReferenceDataAsync` is copied again in `IntegrationTestWebAppFactory`. Three separate copies of the same org+user seeding logic exist. | Consolidate into a single `TestDataSeeder` class in Tests.Common. | 0.9 |
| RateLimitTestWebAppFactory.cs | 49-58 | medium | DRY | Duplicated `ReplaceService` helper | `ReplaceService<TInterface, TImplementation>` is copy-pasted from `IntegrationTestWebAppFactory`. | Extract to a shared `ServiceCollectionTestExtensions` static class. | 0.9 |
| RateLimitTestWebAppFactory.cs | 60-75 | medium | DRY | Duplicated `ReplaceHealthCheck` helper | `ReplaceHealthCheck` method is identical to the one in `IntegrationTestWebAppFactory`. | Same extraction as above. | 0.9 |
| StaleItemDetectorJobTests.cs | 59 | medium | code-smell | Mutable field `_currentProject` set in helper | `_currentProject` is a mutable instance field set as a side effect of `AddProjectAsync()`. Implicit state coupling between helper and tests. | Return `Project` from `AddProjectAsync` instead of setting a field. | 0.8 |
| StaleItemDetectorJobTests.cs | 45-57 | low | DRY / builder-bypass | Inline entity graph construction in helper | `AddProjectAsync` constructs org+project inline. Could use builders. | Use `OrganizationBuilder` and `ProjectBuilder`. | 0.6 |
| TeamFlowHubTests.cs | 58 | low | dead-code | Unused `_workItemRepo` field | `_workItemRepo` is declared and injected into the hub but never asserted against in any test. | Remove if not needed for hub constructor, or add tests that verify its usage. | 0.7 |
| BackgroundServices TestDbContextFactory | 34 | medium | OCP-violation | `TestTeamFlowDbContext` inherits non-sealed `TeamFlowDbContext` | `TeamFlowDbContext` is not sealed (legitimate base), but `TestTeamFlowDbContext` itself should be `sealed` per CLAUDE.md -- it already is. No issue. However `TestDbContextFactory` is `internal static` (implicitly sealed). Acceptable. | No action. | 0.3 |
| SprintCompletedConsumerTests.cs | 42-48 | low | DRY | Repeated event construction across 3 test methods | Identical `SprintCompletedDomainEvent` construction in 3 tests with same values. | Extract a `CreateTestEvent(Sprint sprint)` helper method. | 0.7 |
| SprintStartedConsumerTests.cs | 49-56 | low | DRY | Repeated event construction across 3 test methods | Same pattern: identical `SprintStartedDomainEvent` construction repeated. | Extract helper method. | 0.7 |
| Application.Tests (multiple) | - | low | DRY | Repeated mock setup pattern across handler tests | Every handler test class repeats the same `_currentUser.Id.Returns(...)` + `_permissions.HasPermissionAsync(...).Returns(true)` setup. At least 8 classes share this exact pattern. | Create a `MockSetupExtensions` class in Tests.Common with `SetupDefaultPermissions()`. | 0.6 |
| ProjectBuilder.cs | 10 | low | type-safety | Status field is `string` not enum | `_status` is a magic string `"Active"` / `"Archived"` rather than a strongly-typed enum. The `Project.Status` field itself is a string, so the builder mirrors it, but this propagates weak typing into tests. | Consider introducing a `ProjectStatus` enum in Domain and updating the builder. | 0.5 |

### Summary

The test projects are generally well-structured with good use of sealed classes, builders, and the Theory/InlineData pattern per CLAUDE.md conventions. The most significant quality issues are (1) triple-duplication of seed/reference data logic across `IntegrationTestBase`, `ApiIntegrationTestBase`, and `IntegrationTestWebAppFactory`, and (2) the `PermissionCheckerTests` class with 85 lines of raw entity construction that bypasses existing builders and violates SRP.

### Unresolved Questions
- Should `Project.Status` be migrated from `string` to a `ProjectStatus` enum to eliminate magic strings in builders and tests?
- Should builders gain a `WithId(Guid)` method to support seeding scenarios that currently use EF Core reflection hacks?
