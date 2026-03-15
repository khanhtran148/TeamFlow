"use client";

import { useProjectContext } from "@/lib/contexts/project-context";
import { useKanbanBoard } from "@/lib/hooks/use-kanban";
import { useKanbanFilterStore } from "@/lib/stores/kanban-filter-store";
import { KanbanToolbar } from "@/components/kanban/kanban-toolbar";
import { KanbanBoard } from "@/components/kanban/kanban-board";
import { EmptyState } from "@/components/shared/empty-state";
import { ErrorDisplay } from "@/components/shared/error-display";
import { Kanban } from "lucide-react";
import type { GetKanbanParams } from "@/lib/api/types";

export default function BoardPage() {
  const { project } = useProjectContext();
  const { filters } = useKanbanFilterStore();

  const params: GetKanbanParams = {
    projectId: project.id,
    ...(filters.type && { type: filters.type }),
    ...(filters.priority && { priority: filters.priority }),
    ...(filters.assigneeId && { assigneeId: filters.assigneeId }),
    ...(filters.releaseId && { releaseId: filters.releaseId }),
    ...(filters.swimlane !== "none" && { swimlane: filters.swimlane }),
  };

  const { data: board, isLoading, isError, error, refetch } = useKanbanBoard(params);

  if (isLoading) {
    return (
      <div style={{ padding: "20px" }}>
        <KanbanToolbar />
        <KanbanLoadingSkeleton />
      </div>
    );
  }

  if (isError || !board) {
    return (
      <div style={{ padding: "20px" }}>
        <KanbanToolbar />
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            marginTop: 12,
          }}
        >
          <ErrorDisplay
            error={error ?? new Error("Board data unavailable")}
            title="Failed to load board"
            onRetry={() => void refetch()}
          />
        </div>
      </div>
    );
  }

  const totalItems = board.columns.reduce((sum, col) => sum + col.itemCount, 0);

  if (totalItems === 0) {
    return (
      <div style={{ display: "flex", flexDirection: "column", height: "100%" }}>
        <KanbanToolbar />
        <div
          style={{
            flex: 1,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <EmptyState
            icon={Kanban}
            title="No items on the board"
            description="Create work items in the backlog to see them here."
          />
        </div>
      </div>
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
      <KanbanToolbar />
      <div style={{ flex: 1, overflow: "auto" }}>
        <KanbanBoard board={board} onRefresh={() => void refetch()} />
      </div>
    </div>
  );
}

function KanbanLoadingSkeleton() {
  return (
    <div
      style={{
        display: "flex",
        gap: 12,
        padding: "16px 0",
      }}
    >
      {[1, 2, 3, 4].map((col) => (
        <div
          key={col}
          style={{
            flex: "1 1 0",
            minWidth: 220,
          }}
        >
          {/* Column header skeleton */}
          <div
            style={{
              height: 36,
              background: "var(--tf-bg3)",
              borderRadius: 6,
              marginBottom: 8,
              animation: "pulse 1.5s ease-in-out infinite",
            }}
          />
          {/* Card skeletons */}
          {[1, 2, 3].map((card) => (
            <div
              key={card}
              style={{
                height: 80,
                background: "var(--tf-bg3)",
                borderRadius: 6,
                marginBottom: 6,
                animation: "pulse 1.5s ease-in-out infinite",
                animationDelay: `${card * 0.1}s`,
              }}
            />
          ))}
        </div>
      ))}
    </div>
  );
}
