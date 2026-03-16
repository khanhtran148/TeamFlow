# Testcontainers Migration Plan

**Date:** 2026-03-16
**Scope:** Migrate all backend tests from NSubstitute mocks + SQLite in-memory to Testcontainers with real PostgreSQL
**Target projects:** Application.Tests (133 files), BackgroundServices.Tests (7 files), Api.Tests Hub test (1 file)

---

## Current State Summary

- **9 files** have zero NSubstitute usage (validators, PermissionMatrix, PagedResult, ValidationBehavior) -- these stay as-is.
- **124 files** use NSubstitute to mock repositories, ICurrentUser, IPermissionChecker, IPublisher, etc.
- **48 files** use `.Received()` mock verification (history recording, event publishing, repository calls).
- **IntegrationTestBase** already exists in Tests.Common with Testcontainers PostgreSQL, but only registers 9 of 27 repositories.
- **BackgroundServices.Tests** uses SQLite in-memory via `TestDbContextFactory` with a custom `TestTeamFlowDbContext` to work around PostgreSQL-specific column types.

### What stays mocked (NSubstitute)
- `IPublisher` -- domain event publishing verification is a cross-cutting concern, not a DB operation.
- `IBroadcastService` -- SignalR broadcasting is external I/O.
- `IAuthService` -- JWT generation/hashing is external crypto, not DB.
- `ILogger<T>` -- logging verification stays mocked.
- `IJobExecutionContext` (Quartz) -- framework object.

### What moves to real PostgreSQL
- All `I*Repository` implementations (27 repositories).
- `IHistoryService` -- already has real `HistoryService` in Tests.Common.
- `IPermissionChecker` -- use `AlwaysAllowTestPermissionChecker` (already exists) for happy path; create `AlwaysDenyTestPermissionChecker` for forbidden tests.
- `ICurrentUser` -- use `TestCurrentUser` (already exists).

---

## Architecture Decision: MediatR Sender vs Direct Handler Construction

**Decision: Use `ISender.Send()` through MediatR pipeline.**

Rationale:
1. Tests exercise the full pipeline (validation behavior, handler).
2. DI resolves all dependencies -- no manual wiring of 4-6 mocks per handler.
3. Matches production code path.
4. `ConfigureServices` override on IntegrationTestBase allows per-test-class customization (e.g., deny permission checker, mock publisher).

---

## Performance Strategy

### Problem
133 test classes each spinning up a PostgreSQL container = slow CI and heavy resource usage.

### Solution: Shared Container via xUnit Collection Fixture

Create `PostgresCollectionFixture` implementing `IAsyncLifetime` that starts ONE container, shared across all test classes in the same collection. Each test class gets its own `IServiceScope` and runs inside a transaction that rolls back after each test.

```
PostgresCollectionFixture (1 container, 1 DB)
  -> ApplicationTestBase (per-class scope, transaction-per-test)
    -> CreateWorkItemTests
    -> AssignWorkItemTests
    -> ...
```

**Transaction-per-test isolation:** Each test begins a transaction via `DbContext.Database.BeginTransactionAsync()`, and rolls it back in `DisposeAsync()`. This gives full isolation without container overhead.

**Estimated speedup:** From ~133 container startups (each 2-4 seconds) to 1 startup. Total test time drops from ~6-8 minutes to ~30-60 seconds.

### Parallel Execution
xUnit runs test classes within a collection sequentially by default. To maximize parallelism:
- Group tests into 4-6 collections (by feature domain), each with its own container.
- Collections run in parallel with each other.

Proposed collections:
1. `WorkItems` (WorkItems, Backlog, Kanban, Search) -- ~36 files
2. `Projects` (Projects, ProjectMemberships, Teams, OrgMembers) -- ~25 files
3. `Sprints` (Sprints, Releases, Dashboard, Reports) -- ~31 files
4. `Social` (Comments, Notifications, Retros, PlanningPoker) -- ~17 files
5. `Auth` (Auth, Admin, Organizations, Invitations, Users) -- ~24 files

5 containers, each shared across ~20-35 test classes, all 5 running in parallel.

---

## Phases

### Phase 0: No-Change Files (0 effort)

**9 files that require NO migration** -- they test pure logic with no mocks:
- `PermissionMatrixTests.cs`
- `PagedResultTests.cs`
- `ValidationBehaviorTests.cs`
- `FullTextSearchValidatorTests.cs`
- `SaveFilterValidatorTests.cs`
- `UpdateSavedFilterValidatorTests.cs`
- `SubmitRetroCardValidatorTests.cs`
- `UpdateProfileValidatorTests.cs`
- `UpdatePreferencesValidatorTests.cs`

These stay exactly as-is. Validator tests instantiate validators directly and test FluentValidation rules. No DB needed.

---

### Phase 1: Infrastructure Setup (~10 files)

**Goal:** Build the shared test infrastructure that all migrated tests depend on.

**FILE OWNERSHIP:**
- `tests/TeamFlow.Tests.Common/IntegrationTestBase.cs` -- MODIFY
- `tests/TeamFlow.Tests.Common/ApplicationTestBase.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/PostgresCollectionFixture.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/Collections/WorkItemsCollection.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/Collections/ProjectsCollection.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/Collections/SprintsCollection.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/Collections/SocialCollection.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/Collections/AuthCollection.cs` -- CREATE
- `tests/TeamFlow.Tests.Common/TestStubs.cs` -- MODIFY (add AlwaysDenyTestPermissionChecker, CapturingPublisher)
- `tests/TeamFlow.Tests.Common/TeamFlow.Tests.Common.csproj` -- MODIFY (add MediatR if needed)
- `tests/TeamFlow.Application.Tests/TeamFlow.Application.Tests.csproj` -- MODIFY (add Infrastructure reference)

#### 1a. PostgresCollectionFixture

Shared container per collection. Starts one PostgreSQL container, creates schema, seeds base data.

```csharp
public sealed class PostgresCollectionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("teamflow_test")
        .WithUsername("teamflow_test")
        .WithPassword("teamflow_test")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Create schema once
        using var scope = BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedReferenceDataAsync(db, SeedOrgId, SeedUserId);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

#### 1b. ApplicationTestBase

Per-test-class base that uses the shared fixture. Each test runs inside a rolled-back transaction.

```csharp
public abstract class ApplicationTestBase : IAsyncLifetime
{
    private readonly PostgresCollectionFixture _fixture;
    private IServiceScope _scope = null!;
    private IDbContextTransaction _transaction = null!;

    protected ISender Sender => _scope.ServiceProvider.GetRequiredService<ISender>();
    protected TeamFlowDbContext DbContext => _scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();

    protected ApplicationTestBase(PostgresCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        // Register all services using fixture.ConnectionString
        RegisterAllServices(services, _fixture.ConnectionString);
        ConfigureServices(services); // per-test-class override
        var provider = services.BuildServiceProvider();
        _scope = provider.CreateScope();
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    protected virtual void ConfigureServices(IServiceCollection services) { }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        _scope.Dispose();
    }
}
```

#### 1c. Register ALL Repositories

Expand `RegisterCommonServices` to include every repository:

Missing repositories to add:
- `ICommentRepository` -> `CommentRepository`
- `ITeamRepository` -> `TeamRepository`
- `ISprintRepository` -> `SprintRepository`
- `IRefreshTokenRepository` -> `RefreshTokenRepository`
- `IInAppNotificationRepository` -> `InAppNotificationRepository`
- `INotificationPreferenceRepository` -> `NotificationPreferenceRepository`
- `ISavedFilterRepository` -> `SavedFilterRepository`
- `ISprintReportRepository` -> `SprintReportRepository`
- `ITeamHealthSummaryRepository` -> `TeamHealthSummaryRepository`
- `IPlanningPokerSessionRepository` -> `PlanningPokerSessionRepository`
- `IRetroSessionRepository` -> `RetroSessionRepository`
- `IWorkItemHistoryRepository` -> `WorkItemHistoryRepository`
- `IDashboardRepository` -> `DashboardRepository`
- `IBurndownDataPointRepository` -> `BurndownDataPointRepository`
- `ITeamMemberRepository` -> `TeamMemberRepository`
- `IActivityLogRepository` -> `ActivityLogRepository`
- `IEmailOutboxRepository` -> `EmailOutboxRepository`
- `ISprintSnapshotRepository` -> `SprintSnapshotRepository`

#### 1d. CapturingPublisher

For tests that verify domain event publishing, create a test double that records published events:

```csharp
public sealed class CapturingPublisher : IPublisher
{
    private readonly List<object> _published = [];
    public IReadOnlyList<object> Published => _published;

    public Task Publish(object notification, CancellationToken ct = default)
    {
        _published.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        _published.Add(notification);
        return Task.CompletedTask;
    }

    public bool HasPublished<T>() => _published.OfType<T>().Any();
    public T GetPublished<T>() => _published.OfType<T>().Single();
    public IEnumerable<T> GetAllPublished<T>() => _published.OfType<T>();
}
```

#### 1e. AlwaysDenyTestPermissionChecker

For forbidden-path tests:

```csharp
public sealed class AlwaysDenyTestPermissionChecker : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, Guid projectId, Permission permission, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<ProjectRole?> GetEffectiveRoleAsync(Guid userId, Guid projectId, CancellationToken ct = default)
        => Task.FromResult<ProjectRole?>(null);
}
```

#### 1f. Add Project Reference

Application.Tests.csproj must reference TeamFlow.Infrastructure to access repository implementations:

```xml
<ProjectReference Include="..\..\src\core\TeamFlow.Infrastructure\TeamFlow.Infrastructure.csproj" />
```

#### 1g. Validation

- Migrate 2-3 representative test files as proof-of-concept.
- Run full test suite to confirm no regressions.

---

### Phase 2: Migrate Application.Tests -- Batch 1: WorkItems + Backlog + Kanban + Search (~36 files)

**Goal:** Migrate the largest feature group first. Establishes patterns for all subsequent batches.

**FILE OWNERSHIP:** All files in:
- `tests/TeamFlow.Application.Tests/Features/WorkItems/` (13 files)
- `tests/TeamFlow.Application.Tests/Features/Backlog/` (4 files)
- `tests/TeamFlow.Application.Tests/Features/Kanban/` (1 file)
- `tests/TeamFlow.Application.Tests/Features/Search/` (8 files, minus 3 validator-only = 5 files migrated)

**Collection:** `[Collection("WorkItems")]`

**Pattern Transformations:**

**BEFORE -- Mock setup + handler construction:**
```csharp
public sealed class AssignWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    // ... 4 more mocks

    private AssignWorkItemHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidAssignment_SetsAssignee()
    {
        var item = WorkItemBuilder.New().WithType(WorkItemType.Task).Build();
        _workItemRepo.GetByIdAsync(item.Id, ...).Returns(item);
        _workItemRepo.UserExistsAsync(assigneeId, ...).Returns(true);
        _workItemRepo.UpdateAsync(...).Returns(ci => ci.Arg<WorkItem>());

        var result = await CreateHandler().Handle(
            new AssignWorkItemCommand(item.Id, assigneeId), CancellationToken.None);
    }
}
```

**AFTER -- Real DB + MediatR pipeline:**
```csharp
[Collection("WorkItems")]
public sealed class AssignWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidAssignment_SetsAssignee()
    {
        // Insert real data
        var project = ProjectBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Projects.Add(project);
        var item = WorkItemBuilder.New().WithProject(project.Id).AsTask().Build();
        DbContext.WorkItems.Add(item);
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AssignWorkItemCommand(item.Id, assignee.Id));

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.WorkItems.FindAsync(item.Id);
        updated!.AssigneeId.Should().Be(assignee.Id);
    }
}
```

**BEFORE -- Mock verification (.Received):**
```csharp
await _historyService.Received(1).RecordAsync(
    Arg.Is<WorkItemHistoryEntry>(e => e.FieldName == "AssigneeId"),
    Arg.Any<CancellationToken>());
```

**AFTER -- DB state assertion:**
```csharp
var history = await DbContext.WorkItemHistories
    .Where(h => h.WorkItemId == item.Id && h.FieldName == "AssigneeId")
    .SingleAsync();
history.OldValue.Should().Be(prevAssigneeId.ToString());
history.NewValue.Should().Be(newAssigneeId.ToString());
```

**BEFORE -- Permission denial test:**
```csharp
_permissions.HasPermissionAsync(UserId, ProjectId, Permission.Comment_Create, ...)
    .Returns(false);
```

**AFTER -- Override permission checker:**
```csharp
[Collection("WorkItems")]
public sealed class AssignWorkItemForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        // ... setup ...
        var result = await Sender.Send(new AssignWorkItemCommand(item.Id, assigneeId));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
```

**BEFORE -- Publisher verification:**
```csharp
await _publisher.Received(1).Publish(
    Arg.Any<WorkItemCreatedDomainEvent>(), Arg.Any<CancellationToken>());
```

**AFTER -- CapturingPublisher:**
```csharp
protected CapturingPublisher Publisher { get; private set; } = null!;

protected override void ConfigureServices(IServiceCollection services)
{
    var publisher = new CapturingPublisher();
    Publisher = publisher;
    services.AddSingleton<IPublisher>(publisher);
}

[Fact]
public async Task Handle_ValidCommand_PublishesDomainEvent()
{
    // ... setup + send ...
    Publisher.HasPublished<WorkItemCreatedDomainEvent>().Should().BeTrue();
}
```

**Validator tests within these folders that have NO mocks (3 files) stay untouched.**

---

### Phase 3: Migrate Application.Tests -- Batch 2: Projects + Teams + Memberships (~25 files)

**FILE OWNERSHIP:**
- `tests/TeamFlow.Application.Tests/Features/Projects/` (6 files)
- `tests/TeamFlow.Application.Tests/Features/ProjectMemberships/` (4 files)
- `tests/TeamFlow.Application.Tests/Features/Teams/` (8 files)
- `tests/TeamFlow.Application.Tests/Features/OrgMembers/` (3 files)
- `tests/TeamFlow.Application.Tests/Features/Organizations/` (5 files -- minus ListOrganizationsTests if pure)

**Collection:** `[Collection("Projects")]`

Same transformation patterns as Phase 2. Key difference: these tests involve entity graphs (Project -> Team -> Members -> Permissions) that require more seed data setup.

**Helper methods** to add in ApplicationTestBase:
```csharp
protected async Task<Project> SeedProjectAsync(Action<ProjectBuilder>? configure = null)
{
    var builder = ProjectBuilder.New().WithOrganization(SeedOrgId);
    configure?.Invoke(builder);
    var project = builder.Build();
    DbContext.Projects.Add(project);
    await DbContext.SaveChangesAsync();
    return project;
}

protected async Task<WorkItem> SeedWorkItemAsync(Guid projectId, Action<WorkItemBuilder>? configure = null)
{
    var builder = WorkItemBuilder.New().WithProject(projectId);
    configure?.Invoke(builder);
    var item = builder.Build();
    DbContext.WorkItems.Add(item);
    await DbContext.SaveChangesAsync();
    return item;
}
```

---

### Phase 4: Migrate Application.Tests -- Batch 3: Sprints + Releases + Dashboard + Reports (~31 files)

**FILE OWNERSHIP:**
- `tests/TeamFlow.Application.Tests/Features/Sprints/` (11 files)
- `tests/TeamFlow.Application.Tests/Features/Releases/` (10 files)
- `tests/TeamFlow.Application.Tests/Features/Dashboard/` (6 files)
- `tests/TeamFlow.Application.Tests/Features/Reports/` (4 files)

**Collection:** `[Collection("Sprints")]`

**Special consideration:** Sprint lifecycle tests (Start, Complete) involve state machines and side effects. These benefit most from real DB because the mock setup was fragile.

---

### Phase 5: Migrate Application.Tests -- Batch 4: Social + Auth + Admin (~41 files)

**FILE OWNERSHIP:**
- `tests/TeamFlow.Application.Tests/Features/Comments/` (4 files)
- `tests/TeamFlow.Application.Tests/Features/Notifications/` (7 files, minus 1 validator = 6 migrated)
- `tests/TeamFlow.Application.Tests/Features/Retros/` (10 files, minus 1 validator = 9 migrated)
- `tests/TeamFlow.Application.Tests/Features/PlanningPoker/` (1 file)
- `tests/TeamFlow.Application.Tests/Features/Auth/` (5 files)
- `tests/TeamFlow.Application.Tests/Features/Admin/` (9 files)
- `tests/TeamFlow.Application.Tests/Features/Invitations/` (5 files)
- `tests/TeamFlow.Application.Tests/Features/Users/` (4 files, minus 1 validator = 3 migrated)
- `tests/TeamFlow.Application.Tests/Common/ActiveUserBehaviorTests.cs` (1 file)

**Collections:** `[Collection("Social")]` and `[Collection("Auth")]`

**Special considerations:**
- **Auth tests** (Register, Login, RefreshToken) depend on `IAuthService` for JWT/hashing -- this stays mocked. Tests still move to Testcontainers for the repository layer, but `IAuthService` remains NSubstitute.
- **Admin tests** (ChangeUserStatus, ResetPassword) interact with UserRepository and RefreshTokenRepository -- real DB.
- **Notification tests** involve `IInAppNotificationRepository` with pagination -- real DB validates the SQL.

---

### Phase 6: Migrate BackgroundServices.Tests (~7 files)

**FILE OWNERSHIP:**
- `tests/TeamFlow.BackgroundServices.Tests/TestDbContextFactory.cs` -- DELETE
- `tests/TeamFlow.BackgroundServices.Tests/TestTeamFlowDbContext` (inner class) -- DELETE
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/BurndownSnapshotJobTests.cs` -- MODIFY
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/EventPartitionCreatorJobTests.cs` -- MODIFY
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/ReleaseOverdueDetectorJobTests.cs` -- MODIFY
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/StaleItemDetectorJobTests.cs` -- MODIFY
- `tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintCompletedConsumerTests.cs` -- MODIFY
- `tests/TeamFlow.BackgroundServices.Tests/Consumers/SprintStartedConsumerTests.cs` -- MODIFY
- `tests/TeamFlow.BackgroundServices.Tests/TeamFlow.BackgroundServices.Tests.csproj` -- MODIFY (remove SQLite/InMemory, add Testcontainers)

**Strategy:**
- Replace `TestDbContextFactory.Create()` with a shared `PostgresCollectionFixture`.
- Remove the `TestTeamFlowDbContext` class that hacked around PostgreSQL column types.
- Keep NSubstitute for `IBroadcastService`, `ILogger<T>`, `IJobExecutionContext`.
- Jobs construct directly (not through MediatR), so they receive `TeamFlowDbContext` from the fixture.

**csproj changes:**
```xml
<!-- REMOVE -->
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" ... />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" ... />

<!-- ADD (via Tests.Common reference which already has Testcontainers) -->
<ProjectReference Include="..\TeamFlow.Tests.Common\TeamFlow.Tests.Common.csproj" />
```

---

### Phase 7: Hub Test (1 file) -- Low Priority

**FILE OWNERSHIP:**
- `tests/TeamFlow.Api.Tests/Hubs/TeamFlowHubTests.cs` -- EVALUATE

**Decision: Keep as-is.**

The Hub test mocks `IHubCallerClients`, `IGroupManager`, `HubCallerContext` -- these are SignalR framework objects that cannot be replaced with real implementations in a unit test. The repository mocks (`IWorkItemRepository`, `IRetroSessionRepository`) in this file are not exercised in the current tests (they test `JoinProject` which only uses `IPermissionChecker`). Migration would add container overhead with no testing benefit.

If Hub tests expand to test data-fetching methods, they should move to Api.Tests E2E tests with `IntegrationTestWebAppFactory`.

---

### Phase 8: Cleanup + Verification

- Remove NSubstitute package from `Application.Tests.csproj` if no tests reference it anymore (Auth tests may still need it for `IAuthService`).
- Remove `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.InMemory` from `BackgroundServices.Tests.csproj`.
- Run full test suite, verify all pass.
- Check test execution time. Target: under 2 minutes for full suite.
- Update `CLAUDE.md` if any conventions changed.

---

## File Count Summary

| Phase | Files Modified/Created | Description |
|-------|----------------------|-------------|
| 0     | 0                    | 9 files stay as-is (validators, pure logic) |
| 1     | ~10                  | Infrastructure (base classes, fixtures, stubs, csproj) |
| 2     | ~23                  | WorkItems + Backlog + Kanban + Search |
| 3     | ~25                  | Projects + Teams + Memberships + Orgs |
| 4     | ~31                  | Sprints + Releases + Dashboard + Reports |
| 5     | ~38                  | Social + Auth + Admin + Invitations + Users |
| 6     | ~8                   | BackgroundServices.Tests |
| 7     | 0                    | Hub test stays as-is |
| 8     | ~3                   | Cleanup (csproj removals, CLAUDE.md) |
| **Total** | **~138**         | |

---

## Risk Analysis

### Risk 1: Transaction rollback breaks EF Core change tracker
**Likelihood:** Medium
**Impact:** Tests see stale data or fail on second assertion.
**Mitigation:** Create a fresh `IServiceScope` per test. Use `DbContext.ChangeTracker.Clear()` before assertions that re-query. If rollback causes issues with `EnsureCreated`, switch to `TRUNCATE` all tables between tests instead.

### Risk 2: Missing repository registrations cause runtime DI failures
**Likelihood:** High (18 repositories are not registered in IntegrationTestBase)
**Impact:** Tests fail at startup with `InvalidOperationException`.
**Mitigation:** Phase 1 registers all 27 repositories upfront. Run a single smoke test before batch migration.

### Risk 3: Seed data dependencies between tests
**Likelihood:** Medium
**Impact:** Test A inserts data that affects Test B's assertions.
**Mitigation:** Transaction-per-test rollback. If insufficient, use `TRUNCATE` + re-seed pattern.

### Risk 4: PostgreSQL-specific behavior differences from mocked repositories
**Likelihood:** Low
**Impact:** Tests that passed with mocks fail with real DB (e.g., case-sensitive string comparisons, null ordering).
**Mitigation:** This is actually a benefit -- these tests were hiding real bugs. Fix the production code if needed.

### Risk 5: Container startup time in CI
**Likelihood:** Medium
**Impact:** CI pipeline takes 3+ minutes just for container init.
**Mitigation:** 5 parallel collection fixtures. Reuse Docker image layer cache. Consider Testcontainers Ryuk settings for CI.

### Risk 6: Auth tests need hybrid approach
**Likelihood:** Certain
**Impact:** Auth handlers use `IAuthService` for JWT/hashing which has no Infrastructure implementation -- it's registered in Api's `Program.cs`.
**Mitigation:** Auth tests use real PostgreSQL for repositories but keep NSubstitute for `IAuthService`. This is explicitly allowed by the scope definition.

### Risk 7: Tests that verify mock call counts have no DB equivalent
**Likelihood:** Medium (48 files use `.Received()`)
**Impact:** Verification tests like "RecordsHistory was called exactly once" cannot be expressed as DB assertions if the side effect doesn't persist.
**Mitigation:**
- History recording -> query `WorkItemHistories` table (persisted).
- Domain event publishing -> use `CapturingPublisher`.
- Repository `AddAsync` calls -> query the table for inserted rows.
- Repository `Received(0)` (never called) -> verify row does NOT exist.

### Risk 8: Shared container breaks parallel test execution
**Likelihood:** Low
**Impact:** Tests within the same collection interfere with each other.
**Mitigation:** Transaction isolation. Each test class gets its own scope and transaction. If two classes run concurrently within a collection (they should not by default in xUnit), add `[CollectionDefinition(DisableParallelization = false)]`.

---

## Migration Checklist (Per Test File)

For each file being migrated, the implementer should:

1. [ ] Change class to extend `ApplicationTestBase` with appropriate collection fixture
2. [ ] Add `[Collection("...")]` attribute
3. [ ] Remove all `Substitute.For<I*Repository>()` fields
4. [ ] Remove `CreateHandler()` method
5. [ ] Replace mock `.Returns()` setup with DB insert using builders
6. [ ] Replace `await CreateHandler().Handle(command, ct)` with `await Sender.Send(command)`
7. [ ] Replace mock `.Received()` assertions with DB state queries
8. [ ] Replace `_permissions.Returns(false)` tests with `AlwaysDenyTestPermissionChecker` override
9. [ ] Replace `_publisher.Received()` with `CapturingPublisher` assertions
10. [ ] Keep NSubstitute for non-DB services (`IAuthService`, `IPublisher` where CapturingPublisher is overkill)
11. [ ] Run tests, verify green

---

## Dependencies Between Phases

```
Phase 1 (Infrastructure)
   |
   +---> Phase 2 (WorkItems batch)
   |        |
   +---> Phase 3 (Projects batch)  -- can run PARALLEL with Phase 2
   |        |
   +---> Phase 4 (Sprints batch)   -- can run PARALLEL with Phase 2, 3
   |        |
   +---> Phase 5 (Social+Auth)     -- can run PARALLEL with Phase 2, 3, 4
   |
   +---> Phase 6 (BackgroundServices) -- can run PARALLEL with Phases 2-5
   |
   Phase 7 (Hub test) -- no action
   |
   Phase 8 (Cleanup)  -- depends on ALL previous phases
```

Phases 2-6 are independent of each other and can be assigned to different implementers working in parallel.
