# Phase 2: Backend -- GetActivityLog

**PARALLEL:** yes (with Phase 3: Frontend)
**Depends on:** API Contract (api-contract-260316-1600.md)
**TFD:** mandatory

---

## Summary

Paginated activity log endpoint that queries `work_item_histories` for the authenticated user's recent actions.

---

## FILE OWNERSHIP

This phase owns all files under:
- `src/core/TeamFlow.Application/Features/Users/GetActivityLog/` (new folder)
- `src/core/TeamFlow.Application/Features/Users/ActivityLogItemDto.cs` (new)
- `src/apps/TeamFlow.Api/Controllers/UsersController.cs` (modify -- add endpoint)
- `tests/TeamFlow.Application.Tests/Features/Users/GetActivityLog/` (new)

**Shared with Phase 1:**
- `src/apps/TeamFlow.Api/Controllers/UsersController.cs` -- Phase 1 adds profile endpoints, Phase 2 adds activity endpoint. If running in parallel, Phase 2 must merge after Phase 1.

---

## Tasks

### 2.1 DTO

**File:** `src/core/TeamFlow.Application/Features/Users/ActivityLogItemDto.cs`

```csharp
public sealed record ActivityLogItemDto(
    Guid Id,
    Guid WorkItemId,
    string WorkItemTitle,
    string ActionType,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt
);
```

### 2.2 GetActivityLog Query (TFD)

**Tests first** (`tests/TeamFlow.Application.Tests/Features/Users/GetActivityLog/GetActivityLogHandlerTests.cs`):
- `Handle_UserWithActivity_ReturnsPaginatedResults`
- `Handle_UserWithNoActivity_ReturnsEmptyPage`
- `Handle_Page2_ReturnsCorrectOffset`
- `Handle_DefaultPageSize_Returns20Items`

**Implementation:**
- `src/core/TeamFlow.Application/Features/Users/GetActivityLog/GetActivityLogQuery.cs`
- `src/core/TeamFlow.Application/Features/Users/GetActivityLog/GetActivityLogHandler.cs`

**Query:**
```csharp
public sealed record GetActivityLogQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<ActivityLogItemDto>>>;
```

**Handler logic:**
1. Get `ICurrentUser.Id`
2. Query `work_item_histories` where `ActorId == currentUser.Id` and `ActorType == "User"`
3. Join with `WorkItem` to get `Title`
4. Order by `CreatedAt` descending
5. Apply pagination
6. Project to `ActivityLogItemDto`
7. Return `PagedResult<ActivityLogItemDto>`

**Repository approach:**
- Use `TeamFlowDbContext` directly in handler (read query, no business logic)
- Or add method to a repository -- either approach acceptable for a read-only projection

### 2.3 Controller Endpoint

Add to `UsersController`:

```csharp
[HttpGet("me/activity")]
[ProducesResponseType(typeof(PagedResult<ActivityLogItemDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetActivityLog(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
```

**File:** `src/apps/TeamFlow.Api/Controllers/UsersController.cs`

---

## Acceptance Criteria

- [ ] `GET /api/v1/users/me/activity?page=1&pageSize=20` returns paginated activity
- [ ] Results ordered by most recent first
- [ ] Each item includes work item title (joined from work_items table)
- [ ] PageSize capped at 50
- [ ] All handler tests pass (TFD)
