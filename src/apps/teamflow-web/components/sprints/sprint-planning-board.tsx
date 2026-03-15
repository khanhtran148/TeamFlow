"use client";

import { useState, useMemo, useCallback } from "react";
import {
  DndContext,
  DragOverlay,
  useDroppable,
  useDraggable,
  type DragStartEvent,
  type DragEndEvent,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import { toast } from "sonner";
import { GripVertical, Lock } from "lucide-react";
import { useBacklog } from "@/lib/hooks/use-backlog";
import {
  useSprint,
  useAddItemToSprint,
  useRemoveItemFromSprint,
} from "@/lib/hooks/use-sprints";
import { useHasPermission } from "@/lib/hooks/use-permission";
import { CapacityIndicator } from "./capacity-indicator";
import { MemberCapacity } from "./member-capacity";
import { AddItemConfirmation } from "./add-item-confirmation";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import { PriorityIcon } from "@/components/shared/priority-icon";
import { Skeleton } from "@/components/ui/skeleton";
import type { BacklogItemDto, WorkItemDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface SprintPlanningBoardProps {
  sprintId: string;
  projectId: string;
}

const BACKLOG_DROPPABLE = "backlog-panel";
const SPRINT_DROPPABLE = "sprint-panel";

// ---- Draggable Item Row ----

function DraggableItem({
  item,
  isDragOverlay,
}: {
  item: BacklogItemDto | WorkItemDto;
  isDragOverlay?: boolean;
}) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: item.id,
    data: { item },
  });

  const style: React.CSSProperties = {
    display: "flex",
    alignItems: "center",
    gap: 8,
    padding: "8px 12px",
    background: isDragOverlay ? "var(--tf-bg3)" : "transparent",
    borderBottom: "1px solid var(--tf-border)",
    opacity: isDragging && !isDragOverlay ? 0.3 : 1,
    cursor: "grab",
    transition: "background var(--tf-tr)",
    ...(isDragOverlay
      ? {
          border: "1px solid var(--tf-accent)",
          borderRadius: 6,
          boxShadow: "var(--tf-shadow)",
        }
      : {}),
  };

  return (
    <div
      ref={isDragOverlay ? undefined : setNodeRef}
      style={style}
      {...(isDragOverlay ? {} : attributes)}
      {...(isDragOverlay ? {} : listeners)}
      onMouseEnter={(e) => {
        if (!isDragOverlay) {
          (e.currentTarget as HTMLDivElement).style.background = "var(--tf-bg3)";
        }
      }}
      onMouseLeave={(e) => {
        if (!isDragOverlay) {
          (e.currentTarget as HTMLDivElement).style.background = "transparent";
        }
      }}
    >
      <GripVertical size={12} color="var(--tf-text3)" style={{ flexShrink: 0 }} />
      <TypeIcon type={item.type} size={13} />
      <span
        style={{
          flex: 1,
          fontSize: 12,
          color: "var(--tf-text)",
          fontWeight: 500,
          overflow: "hidden",
          textOverflow: "ellipsis",
          whiteSpace: "nowrap",
          minWidth: 0,
        }}
      >
        {item.title}
      </span>
      <div style={{ display: "flex", alignItems: "center", gap: 6, flexShrink: 0 }}>
        {item.priority && <PriorityIcon priority={item.priority} />}
        {item.estimationValue !== null && item.estimationValue !== undefined && (
          <span
            style={{
              fontSize: 10,
              fontFamily: "var(--tf-font-mono)",
              color: "var(--tf-text3)",
              background: "var(--tf-bg4)",
              padding: "1px 5px",
              borderRadius: 4,
            }}
          >
            {item.estimationValue}
          </span>
        )}
        <StatusBadge status={item.status} size="sm" />
      </div>
    </div>
  );
}

// ---- Droppable Panel ----

function DroppablePanel({
  id,
  children,
  title,
  count,
  headerAction,
}: {
  id: string;
  children: React.ReactNode;
  title: string;
  count: number;
  headerAction?: React.ReactNode;
}) {
  const { isOver, setNodeRef } = useDroppable({ id });

  return (
    <div
      data-testid={id}
      ref={setNodeRef}
      style={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
        minWidth: 0,
        background: "var(--tf-bg2)",
        border: `1px solid ${isOver ? "var(--tf-accent)" : "var(--tf-border)"}`,
        borderRadius: "var(--tf-radius)",
        overflow: "hidden",
        transition: "border-color var(--tf-tr)",
      }}
    >
      {/* Panel header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "10px 14px",
          borderBottom: "1px solid var(--tf-border)",
          background: isOver ? "var(--tf-accent-dim)" : "transparent",
          transition: "background var(--tf-tr)",
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 600,
              fontSize: 13,
              color: "var(--tf-text)",
            }}
          >
            {title}
          </span>
          <span
            style={{
              padding: "1px 7px",
              borderRadius: 100,
              background: "var(--tf-bg4)",
              color: "var(--tf-text3)",
              fontSize: 11,
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {count}
          </span>
        </div>
        {headerAction}
      </div>

      {/* Panel content */}
      <div
        style={{
          flex: 1,
          overflowY: "auto",
          maxHeight: 500,
        }}
      >
        {children}
      </div>
    </div>
  );
}

// ---- Main Board ----

export function SprintPlanningBoard({ sprintId, projectId }: SprintPlanningBoardProps) {
  const [activeItemId, setActiveItemId] = useState<string | null>(null);
  const [confirmItem, setConfirmItem] = useState<BacklogItemDto | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
  );

  const { data: sprintDetail, isLoading: isSprintLoading } = useSprint(sprintId);
  const { data: backlogData, isLoading: isBacklogLoading } = useBacklog({
    projectId,
    unscheduled: true,
    pageSize: 200,
  });

  const { mutate: addItem, isPending: isAdding } = useAddItemToSprint(projectId);
  const { mutate: removeItem } = useRemoveItemFromSprint(projectId);

  const canEditSprint = useHasPermission(projectId, "Sprint_Edit");
  const isActiveSprint = sprintDetail?.status === "Active";
  const isPlanningStatus = sprintDetail?.status === "Planning";

  const sprintItems = useMemo(() => sprintDetail?.items ?? [], [sprintDetail]);
  const sprintItemIds = useMemo(() => new Set(sprintItems.map((i) => i.id)), [sprintItems]);

  const backlogItems = useMemo(
    () => (backlogData?.items ?? []).filter((item) => !sprintItemIds.has(item.id)),
    [backlogData, sprintItemIds],
  );

  const totalCapacity = useMemo(
    () => (sprintDetail?.capacity ?? []).reduce((sum, m) => sum + m.capacityPoints, 0),
    [sprintDetail],
  );

  const assignedPoints = useMemo(
    () => sprintItems.reduce((sum, item) => sum + (item.estimationValue ?? 0), 0),
    [sprintItems],
  );

  const allItems = useMemo(() => {
    const map = new Map<string, BacklogItemDto | WorkItemDto>();
    for (const item of backlogItems) map.set(item.id, item);
    for (const item of sprintItems) map.set(item.id, item);
    return map;
  }, [backlogItems, sprintItems]);

  const activeItem = activeItemId ? allItems.get(activeItemId) ?? null : null;

  const handleDragStart = useCallback((event: DragStartEvent) => {
    setActiveItemId(event.active.id as string);
  }, []);

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      setActiveItemId(null);
      const { active, over } = event;
      if (!over || !canEditSprint) return;

      const itemId = active.id as string;
      const isInSprint = sprintItemIds.has(itemId);
      const targetPanel = over.id as string;

      // Dragging from backlog to sprint
      if (!isInSprint && targetPanel === SPRINT_DROPPABLE) {
        if (isActiveSprint) {
          // Show confirmation for active sprint
          const item = allItems.get(itemId) as BacklogItemDto | undefined;
          if (item) {
            setConfirmItem(item);
          }
        } else if (isPlanningStatus) {
          addItem(
            { sprintId, workItemId: itemId },
            {
              onError: (err) => {
                const apiErr = err as ApiError;
                toast.error(apiErr.message ?? "Failed to add item to sprint.");
              },
            },
          );
        }
      }

      // Dragging from sprint to backlog
      if (isInSprint && targetPanel === BACKLOG_DROPPABLE) {
        removeItem(
          { sprintId, workItemId: itemId },
          {
            onError: (err) => {
              const apiErr = err as ApiError;
              toast.error(apiErr.message ?? "Failed to remove item from sprint.");
            },
          },
        );
      }
    },
    [
      canEditSprint,
      sprintItemIds,
      isActiveSprint,
      isPlanningStatus,
      addItem,
      removeItem,
      sprintId,
      allItems,
    ],
  );

  function handleConfirmAdd() {
    if (!confirmItem) return;
    addItem(
      { sprintId, workItemId: confirmItem.id },
      {
        onSuccess: () => {
          setConfirmItem(null);
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to add item to sprint.");
          setConfirmItem(null);
        },
      },
    );
  }

  if (isSprintLoading || isBacklogLoading) {
    return (
      <div style={{ display: "flex", gap: 12, minHeight: 300 }}>
        <div style={{ flex: 1 }}>
          <Skeleton style={{ height: "100%", borderRadius: 8, minHeight: 300 }} />
        </div>
        <div style={{ flex: 1 }}>
          <Skeleton style={{ height: "100%", borderRadius: 8, minHeight: 300 }} />
        </div>
      </div>
    );
  }

  return (
    <div data-testid="sprint-planning-board" style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      {/* Capacity indicator */}
      {totalCapacity > 0 && (
        <CapacityIndicator
          assignedPoints={assignedPoints}
          totalCapacity={totalCapacity}
          label="Sprint Capacity"
        />
      )}

      {/* Scope lock indicator */}
      {isActiveSprint && (
        <div
          role="alert"
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "8px 14px",
            background: "var(--tf-yellow-dim)",
            border: "1px solid var(--tf-yellow)",
            borderRadius: 6,
            fontSize: 12,
            color: "var(--tf-yellow)",
            fontWeight: 500,
          }}
        >
          <Lock size={13} />
          Sprint is active. Scope changes require Team Manager approval.
        </div>
      )}

      {/* DnD split view */}
      <DndContext
        sensors={sensors}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          style={{
            display: "flex",
            gap: 12,
            minHeight: 300,
            flexDirection: "row",
          }}
        >
          {/* Backlog panel (left) */}
          <DroppablePanel
            id={BACKLOG_DROPPABLE}
            title="Backlog"
            count={backlogItems.length}
          >
            {backlogItems.length === 0 ? (
              <div
                style={{
                  padding: "24px 16px",
                  textAlign: "center",
                  color: "var(--tf-text3)",
                  fontSize: 12,
                }}
              >
                No unscheduled items in backlog.
              </div>
            ) : (
              backlogItems.map((item) => (
                <DraggableItem key={item.id} item={item} />
              ))
            )}
          </DroppablePanel>

          {/* Sprint panel (right) */}
          <DroppablePanel
            id={SPRINT_DROPPABLE}
            title={sprintDetail?.name ?? "Sprint"}
            count={sprintItems.length}
          >
            {sprintItems.length === 0 ? (
              <div
                style={{
                  padding: "24px 16px",
                  textAlign: "center",
                  color: "var(--tf-text3)",
                  fontSize: 12,
                }}
              >
                Drag items from the backlog to plan this sprint.
              </div>
            ) : (
              sprintItems.map((item) => (
                <DraggableItem key={item.id} item={item} />
              ))
            )}
          </DroppablePanel>
        </div>

        <DragOverlay>
          {activeItem ? <DraggableItem item={activeItem} isDragOverlay /> : null}
        </DragOverlay>
      </DndContext>

      {/* Member capacity breakdown */}
      {sprintDetail && sprintDetail.capacity.length > 0 && (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            padding: 14,
          }}
        >
          <MemberCapacity members={sprintDetail.capacity} />
        </div>
      )}

      {/* Active sprint add item confirmation */}
      <AddItemConfirmation
        open={confirmItem !== null}
        itemTitle={confirmItem?.title ?? ""}
        isPending={isAdding}
        onConfirm={handleConfirmAdd}
        onCancel={() => setConfirmItem(null)}
      />
    </div>
  );
}
