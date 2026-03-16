## Performance Review — 260316

**Status**: COMPLETED
**Concern**: performance
**Files reviewed**: 23 (13 backend, 10 frontend)

### Findings

| File | Line | Severity | Category | Title | Detail | Suggestion | Confidence |
|------|------|----------|----------|-------|--------|------------|------------|
| `BulkUpdatePriorityHandler.cs` | 16–36 | High | N+1 Query | N+1 per item in bulk update loop | Each iteration calls `GetByIdAsync` + `UpdateAsync` + `historyService.RecordAsync` — 3 DB round-trips per item. 50 items = 150 queries. | Batch-fetch all work items with a single `WHERE Id IN (...)` query, then loop; batch-insert history rows; use `ExecuteUpdateAsync` for priority update. | 0.98 |
| `WorkItemRepository.cs` | 93–104 | High | N+1 Query | Recursive `CollectDescendantsAsync` issues N+1 per depth level | Each level of hierarchy spawns a new `SELECT WHERE ParentId = ?`. For a 3-level hierarchy the cascade delete fires a new query per node. | Replace with a single CTE recursive query: `WITH RECURSIVE descendants AS (...)`. | 0.95 |
| `GetReleaseDetailHandler.cs` | 24–25 | Medium | Missing filter push-down | `isReady` / `isBlocked` filters not applied in grouped release view | `GetBacklogPagedAsync` is called with `pageSize: 1000` and no `isBlocked`/`isReady` predicate; in-memory LINQ is then used for grouping. With large releases this materialises all 1000 items. | Add a dedicated repository method for release detail that projects only the columns needed for grouping and progress, or accept `isBlocked` as a parameter to avoid over-fetching. | 0.90 |
| `GetBacklogQuery.cs` / `GetBacklogParams` (types.ts) | — | Medium | Missing filter push-down | `readyOnly` filter exists in the Zustand store and toolbar UI but is never sent to the backend | `GetBacklogQuery` and `GetBacklogParams` have no `isReady` parameter; the store's `readyOnly: true` flag is never serialised into the API request. Filtering would have to happen client-side (silently broken). | Add `IsReady?: bool` to `GetBacklogQuery`, propagate it through `GetBacklogPagedAsync`, and add `isReady?: boolean` to `GetBacklogParams` in types.ts and the `getBacklog` API call. | 0.97 |
| `GetBacklogHandler.cs` | 36–37 | Low | Caching opportunity | Blocked-item lookup always re-queries on every paginated page load | `GetBlockedItemIdsAsync` executes per page request even though blocker links rarely change. | Cache blocked IDs per project with a short TTL (e.g. 30 s `IMemoryCache`) invalidated on link create/delete events. | 0.75 |
| `CommentRepository.cs` | 36–49 | Low | Missing projection | Full entity load with two `.Include()` chains for read-only comment list | `GetByWorkItemPagedAsync` loads full `Author` + all `Replies` + their `Authors`; replies are not paginated and could be large for active threads. | Use `.Select(c => new CommentDto {...})` projection or limit reply depth; add a `maxReplies` cap. | 0.80 |
| `GetCommentsHandler.cs` | 17–19 | Low | Extra round-trip | Work item fetched only to resolve `ProjectId` for permission check | Every `GetComments` call does a full `WorkItem` load (with Assignee/Sprint/Release includes) before the permission check. | Add `GetProjectIdAsync(workItemId)` (scalar query) on the repository, or include `ProjectId` in the comment query to avoid the full entity load. | 0.85 |
| `event-handlers.ts` | 228–244 | Low | Excessive cache invalidation | Retro events invalidate both `retroKeys.detail` and `retroKeys.all` on every card/vote event | During an active retro session every vote broadcasts two `invalidateQueries` calls, triggering two refetches for all connected clients. | Invalidate only `retroKeys.detail` during an active session; invalidate `retroKeys.all` only on `RetroSessionStarted` and `RetroSessionClosed`. | 0.80 |
| `WorkItemRepository.cs` | 178–179 | Low | Full-text scan | `Title.Contains(search)` translates to `LIKE '%term%'` — cannot use a B-tree index | Backlog search on 1000+ items with a leading wildcard performs a sequential scan. | Add a GIN index on `to_tsvector('english', title)` and switch to `EF.Functions.ToTsVector(...).Matches(...)` for the search predicate. | 0.88 |

### Summary

The most critical issue is the N+1 loop in `BulkUpdatePriorityHandler` — it fires 3 queries per item and will degrade linearly with bulk size. The `readyOnly` filter is functionally broken: it exists in the UI store but is never sent to the API, so toggling "Ready" in the toolbar silently has no effect on the fetched dataset.

### Unresolved Questions
- Maximum expected reply depth for comment threading (affects severity of unbounded `Replies` include).
- Whether `GetAllDescendantsAsync` (recursive N+1) is called in any Phase 4 code path at cascade delete time, or only in pre-existing paths.
