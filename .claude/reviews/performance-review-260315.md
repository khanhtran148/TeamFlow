## Performance Review — 260315

**Status**: COMPLETED
**Concern**: performance
**Files reviewed**: 9

### Findings

| File | Line | Severity | Category | Title | Detail | Suggestion | Confidence |
|------|------|----------|----------|-------|--------|------------|------------|
| `components/backlog/backlog-list.tsx` | 77–82 | HIGH | Unnecessary work | Duplicate sensor construction | `useSensors`/`useSensor` called directly in `BacklogList` body on every render, duplicating identical setup already in `BacklogDndProvider`. Two separate `DndContext` trees are also created (flat + grouped), each rebuilding sensors on every parent re-render. | Extract sensors into a stable `useMemo` or move them into a single shared provider. | 0.90 |
| `components/backlog/backlog-list.tsx` | 153, 182 | HIGH | Unnecessary re-renders | Unstable `items` array passed to `SortableContext` | `items.map((i) => i.id)` creates a new array reference on every render, causing `SortableContext` to re-register all sortable nodes unnecessarily. | `const itemIds = useMemo(() => items.map(i => i.id), [items])` and pass `itemIds`. | 0.92 |
| `components/backlog/backlog-row.tsx` | 65 | HIGH | Unnecessary re-renders | `BacklogRow` not memoized | Every reorder or parent state change re-renders all visible rows. With 50 items per page this is 50 full React subtree diffs per keystroke or drag move. | Wrap with `React.memo`. `useRouter` and `useParams` inside each row also add router subscription overhead per row. | 0.88 |
| `components/kanban/kanban-dnd-provider.tsx` | 54–70 | HIGH | Algorithmic complexity | `findColumnForItem` / `findItem` not memoized | Both functions scan all columns linearly on every call and are called multiple times per drag event (start, over, end). On a large board this is O(columns × items) per event. | Build a `Map<itemId, {column, item}>` once via `useMemo([board])` and look up in O(1). | 0.87 |
| `components/kanban/kanban-dnd-provider.tsx` | 127–134 | MEDIUM | Unnecessary work | `COLUMN_LABELS` object recreated every render | The record literal is declared inside the component body, so a new object is allocated on every render. | Move to module scope (constant outside the component). | 0.95 |
| `components/backlog/epic-group.tsx` | 76–79 | MEDIUM | Unnecessary work | `totalPoints` reduce not memoized | `items.reduce(...)` runs on every render of `EpicGroup`, including during drag-move pointer events that re-render the list. | `const totalPoints = useMemo(() => items.reduce(...), [items])`. | 0.85 |
| `lib/signalr/event-handlers.ts` | 65–66 | MEDIUM | Cache invalidation | `WorkItem.*` events over-invalidate | All 8 work-item events call `invalidateQueries` on both `backlogKeys.all` AND `["kanban", projectId]` unconditionally. `WorkItem.Reordered` and `WorkItem.Assigned` do not change kanban column data; `WorkItem.StatusChanged` does not need a full backlog refetch if using optimistic updates. Frequent rapid events (e.g. bulk reorders) will storm both queries. | Differentiate handlers by event type; debounce or batch invalidations using `queryClient.invalidateQueries` with a short `setTimeout` coalesce window. | 0.80 |
| `app/layout.tsx` | 6–25 | MEDIUM | Bundle size | Three Google Font families loaded unconditionally | `Syne`, `DM_Sans`, and `DM_Mono` each generate a separate `<link>` preload. DM Mono is a code/monospace font; if only used in a few label spans it adds an unnecessary network round-trip on every page. | Audit actual usage of `--tf-font-mono`; if only decorative, consider inlining the single weight or using a system monospace fallback. | 0.75 |
| `app/projects/[projectId]/backlog/page.tsx` | 57–62 | MEDIUM | Correctness / perf | `useMemo` used for side effect | Lines 57–62 use `useMemo` to call `setLocalItems(null)` — a side effect inside a memoization hook. React may skip or re-run this unpredictably. This can cause stale local state to persist longer than expected, blocking re-fetches from reflecting on screen. | Replace with `useEffect(() => { setLocalItems(null); }, [data?.items])`. | 0.93 |
| `lib/query-client.ts` | 7 | LOW | Caching | Global `staleTime` of 30 s may be too short for static-ish data | Kanban board structure and project metadata change infrequently. A 30 s stale time means every window re-focus or navigation triggers background refetches across all active queries simultaneously. | Set per-query `staleTime` overrides: e.g. `5 * 60 * 1000` for kanban/backlog, keep 30 s for detail views. | 0.70 |

### Summary

The most impactful issues are in the backlog list: `BacklogRow` is unmemoized causing full-list re-renders on every drag pointer event, `SortableContext` receives a new array reference every render, and sensor setup is duplicated across two DndContext trees. The kanban provider's linear item-lookup functions also need O(1) memoized maps to avoid quadratic work as board size grows.

### Unresolved Questions

- What is the p99 item count expected per backlog page in production? Virtualization (e.g. `@tanstack/react-virtual`) may be warranted if pages routinely exceed 50 visible rows.
- Is `DM_Mono` used beyond the few badge components seen? If yes, the font loading finding is low priority.
