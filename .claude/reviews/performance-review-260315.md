## Performance Review — 260315

**Status**: COMPLETED
**Concern**: performance
**Files reviewed**: 5

### Findings

| File | Line | Severity | Category | Title | Detail | Suggestion | Confidence |
|------|------|----------|----------|-------|--------|------------|------------|
| PermissionChecker.cs | 36–84 | High | Multiple DB Roundtrips | 3 sequential DB roundtrips per permission check | `GetEffectiveRoleAsync` issues up to 3 awaited queries in sequence: project lookup, org-admin check, individual membership check. Every permission-guarded endpoint pays this cost on every request, blocking the response until all round-trips complete. | Merge into a single query using a UNION/CTE, or cache the resolved `(userId, projectId) → role` per HTTP request using `IMemoryCache` with a short TTL (30 s). | 0.95 |
| PermissionChecker.cs | 47–53 | Medium | Missing Index (implied) | OrgAdmin cross-table filter may cause index scan | The `AnyAsync` at line 47 filters `ProjectMemberships` joined to `Projects` on `OrgId`. If no index exists on `project_memberships(member_id, member_type, role)` or `projects(org_id)` the planner will scan both tables. | Confirm and add covering indexes on `project_memberships(member_id, member_type, role)` and `projects(org_id)` in the EF Core configuration. | 0.80 |
| RefreshTokenRepository.cs | 11–12 | High | Missing AsNoTracking | Full entity load with change-tracker on hot token path | `GetByTokenHashAsync` loads the full `RefreshToken` entity and attaches it to the EF Core change tracker. This runs on every authenticated request, adding unnecessary tracking overhead. | Add `.AsNoTracking()` and project only the columns required for validation (`TokenHash`, `UserId`, `ExpiresAt`, `RevokedAt`). | 0.90 |
| RefreshTokenRepository.cs | 27–37 | Medium | Bulk Update Inefficiency | `RevokeAllForUserAsync` materialises all tokens before updating | All active tokens for the user are loaded into memory, mutated in a loop, then persisted via `SaveChangesAsync`. This scales linearly with the number of active sessions and issues one `UPDATE` per token. | Replace with a single `ExecuteUpdateAsync` (EF Core 7+): `.Where(...).ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow), ct)`. One SQL statement regardless of token count. | 0.92 |
| WorkItemHistoryRepository.cs | 19–34 | Medium | Extra DB Roundtrip | `CountAsync` + `ToListAsync` are two separate roundtrips per page | Every paginated history request issues a `COUNT(*)` query followed by a data query on the same filter, doubling round-trip latency. At high history volume this is measurable. | Run both queries in parallel using two separate `DbContext` instances (or a scoped factory), or use a window function via raw SQL. Alternatively, eliminate `CountAsync` for non-first pages by returning a `hasMore` flag instead of `totalCount`. | 0.85 |
| client.ts | 69–73 | Medium | Unbounded Queue | `failedQueue` array grows without a size cap during refresh | If the token-refresh call hangs (up to the 30 s axios timeout), all concurrent 401 responses keep pushing callbacks into `failedQueue`. The array is module-level and unbounded, holding request configs and closures in memory. | Enforce a maximum queue depth (e.g. 50); reject entries beyond the limit immediately with a clear error. | 0.75 |
| client.ts | 34, 188 | Low | Redundant JSON.parse | `localStorage` blob parsed twice per outgoing request | `getAccessToken` (request interceptor) and `getRefreshToken` (refresh flow) each independently call `JSON.parse(localStorage.getItem("teamflow-auth"))`. On pages with many parallel API calls the same JSON is parsed multiple times per tick. | Cache the parsed auth state in a module-level variable; invalidate only in `updateStoredTokens` and `clearStoredAuth`. | 0.80 |
| PermissionMatrix.cs | 14 | Low | Static Allocation | One-time spread of all enum values for OrgAdmin | `[.. Enum.GetValues<Permission>()]` runs once at static initialisation. The runtime cost is negligible and there is no ongoing allocation. | No action needed. | 0.40 |

### Summary

The critical bottleneck is `PermissionChecker.cs`, which fires up to 3 sequential database round-trips per permission-guarded request; at team scale (9–15 users making concurrent requests) this will be the dominant latency contributor and risks breaching the 1-second response budget. `RefreshTokenRepository` compounds the issue by loading full tracked entities on the hot authentication path and using a per-row update loop for session revocation, both of which are straightforward to fix with `AsNoTracking` projections and `ExecuteUpdateAsync`.

### Unresolved Questions

- Migration files for `project_memberships` and `refresh_tokens` were not in scope; the composite indexes on `member_id/member_type/role` and `token_hash` should be verified before closing the index-gap findings.
- `DbContext` lifetime was not confirmed in this review; request-scoped role caching is safe only with the default scoped-per-request lifetime.
