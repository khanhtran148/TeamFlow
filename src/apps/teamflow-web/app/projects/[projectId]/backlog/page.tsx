"use client";

import { useState, useMemo, useEffect } from "react";
import { arrayMove } from "@dnd-kit/sortable";
import { toast } from "sonner";
import { useProjectContext } from "@/lib/contexts/project-context";
import { useBacklog } from "@/lib/hooks/use-backlog";
import { useReorderBacklog } from "@/lib/hooks/use-backlog";
import { useBacklogFilterStore } from "@/lib/stores/backlog-filter-store";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { BacklogToolbar } from "@/components/backlog/backlog-toolbar";
import { BacklogList } from "@/components/backlog/backlog-list";
import { CreateWorkItemDialog } from "@/components/work-items/create-work-item-dialog";
import { Pagination } from "@/components/shared/pagination";
import { ErrorDisplay } from "@/components/shared/error-display";
import type { BacklogItemDto, BlockerItemDto, GetBacklogParams } from "@/lib/api/types";

const PAGE_SIZE = 50;

export default function BacklogPage() {
  const { project } = useProjectContext();
  const { filters } = useBacklogFilterStore();
  const debouncedSearch = useDebounce(filters.search, 300);
  const [page, setPage] = useState(1);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);

  // Local state to hold reordered items for optimistic UI
  const [localItems, setLocalItems] = useState<BacklogItemDto[] | null>(null);

  const queryParams: GetBacklogParams = {
    projectId: project.id,
    search: debouncedSearch || undefined,
    type: filters.type || undefined,
    priority: filters.priority || undefined,
    assigneeId: filters.assigneeId || undefined,
    releaseId: filters.releaseId || undefined,
    page,
    pageSize: PAGE_SIZE,
  };

  const { data, isLoading, isError, error, refetch } = useBacklog(queryParams);
  const reorderMutation = useReorderBacklog();

  // Use local items if we have them (after a reorder), otherwise use fetched items
  const displayItems: BacklogItemDto[] = useMemo(() => {
    const fetched = data?.items ?? [];
    if (localItems !== null) return localItems;

    // Apply blocked-only filter client-side (API doesn't filter by isBlocked directly)
    if (filters.blockedOnly) {
      return fetched.filter((item) => item.isBlocked);
    }
    return fetched;
  }, [data, localItems, filters.blockedOnly]);

  // Reset local items when server data changes
  useEffect(() => {
    if (data) {
      setLocalItems(null);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data?.items]);

  // Build a blockers map from items that are blocked
  // In the real app this would come from the API, but we derive it from the isBlocked flag
  // and relying on each item's isBlocked field. For the tooltip, we use placeholder data.
  const blockersMap: Record<string, BlockerItemDto[]> = useMemo(() => {
    const map: Record<string, BlockerItemDto[]> = {};
    // The BacklogItemDto has isBlocked but not the actual blocker details
    // We only show the blocked icon; tooltip info comes from the flag
    for (const item of displayItems) {
      if (item.isBlocked) {
        // Placeholder: the real blocker titles would require an extra API call per item
        // For Phase C, we show the icon and a generic message
        map[item.id] = [
          {
            blockerId: "unknown",
            title: "This item has unresolved blockers",
            status: "InProgress",
          },
        ];
      }
    }
    return map;
  }, [displayItems]);

  function handleReorder(reorderedItems: BacklogItemDto[]) {
    setLocalItems(reorderedItems);

    const sortOrders = reorderedItems.map((item, index) => ({
      workItemId: item.id,
      sortOrder: index + 1,
    }));

    reorderMutation.mutate(
      { projectId: project.id, items: sortOrders },
      {
        onError: () => {
          toast.error("Failed to save reorder");
          setLocalItems(null);
        },
      },
    );
  }

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        overflow: "hidden",
      }}
    >
      <BacklogToolbar onNewItem={() => setCreateDialogOpen(true)} />

      {isError ? (
        <div
          style={{
            flex: 1,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <ErrorDisplay
            error={error}
            title="Failed to load backlog"
            onRetry={() => void refetch()}
          />
        </div>
      ) : (
        <div style={{ flex: 1, overflow: "auto" }}>
          <BacklogList
            items={displayItems}
            blockersMap={blockersMap}
            viewMode={filters.viewMode}
            isLoading={isLoading}
            onReorder={handleReorder}
          />
        </div>
      )}

      {/* Pagination */}
      {data && data.totalCount > PAGE_SIZE && (
        <div
          style={{
            padding: "10px 20px",
            borderTop: "1px solid var(--tf-border)",
            background: "var(--tf-bg2)",
          }}
        >
          <Pagination
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}

      <CreateWorkItemDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        projectId={project.id}
      />
    </div>
  );
}
