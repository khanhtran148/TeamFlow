"use client";

import { useState, useMemo, useCallback, type ReactNode } from "react";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  closestCorners,
  type DragStartEvent,
  type DragEndEvent,
  type DragOverEvent,
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import { toast } from "sonner";
import { getBlockers } from "@/lib/api/work-items";
import { changeStatus } from "@/lib/api/work-items";
import { KanbanCardGhost } from "./kanban-card";
import { ConfirmBlockedDialog } from "./confirm-blocked-dialog";
import type { KanbanBoardDto, KanbanItemDto, WorkItemStatus, BlockerItemDto } from "@/lib/api/types";

const KANBAN_COLUMNS: WorkItemStatus[] = ["ToDo", "InProgress", "InReview", "Done"];

const COLUMN_LABELS: Record<WorkItemStatus, string> = {
  ToDo: "To Do",
  InProgress: "In Progress",
  InReview: "In Review",
  Done: "Done",
  Rejected: "Rejected",
  NeedsClarification: "Needs Clarification",
};

interface PendingDrop {
  itemId: string;
  newStatus: WorkItemStatus;
  blockers: BlockerItemDto[];
}

interface KanbanDndProviderProps {
  board: KanbanBoardDto;
  onBoardUpdate: () => void;
  children: ReactNode;
}

export function KanbanDndProvider({
  board,
  onBoardUpdate,
  children,
}: KanbanDndProviderProps) {
  const [activeItem, setActiveItem] = useState<KanbanItemDto | null>(null);
  const [pendingDrop, setPendingDrop] = useState<PendingDrop | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  // O(1) lookup map: itemId → { item, status }
  const itemMap = useMemo(() => {
    const map = new Map<string, { item: KanbanItemDto; status: WorkItemStatus }>();
    for (const col of board.columns) {
      for (const item of col.items) {
        map.set(item.id, { item, status: col.status });
      }
    }
    return map;
  }, [board]);

  function findColumnForItem(itemId: string): WorkItemStatus | null {
    return itemMap.get(itemId)?.status ?? null;
  }

  function findItem(itemId: string): KanbanItemDto | undefined {
    return itemMap.get(itemId)?.item;
  }

  function handleDragStart(event: DragStartEvent) {
    const item = findItem(String(event.active.id));
    if (item) setActiveItem(item);
  }

  function handleDragOver(_event: DragOverEvent) {
    // Visual feedback is handled by useDroppable in KanbanColumn
  }

  async function handleDragEnd(event: DragEndEvent) {
    setActiveItem(null);

    const { active, over } = event;
    if (!over) return;

    const itemId = String(active.id);
    const overId = String(over.id);

    const sourceColumn = findColumnForItem(itemId);
    if (!sourceColumn) return;

    // Determine target column: over.id can be either a column status or another item id
    let targetColumn: WorkItemStatus | null = null;

    if (KANBAN_COLUMNS.includes(overId as WorkItemStatus)) {
      targetColumn = overId as WorkItemStatus;
    } else {
      targetColumn = findColumnForItem(overId);
    }

    if (!targetColumn || targetColumn === sourceColumn) return;

    const item = findItem(itemId);
    if (!item) return;

    // Check if item is blocked and target is InProgress
    if (item.isBlocked && targetColumn === "InProgress") {
      try {
        const blockersDto = await getBlockers(itemId);
        if (blockersDto.hasUnresolvedBlockers && blockersDto.blockers.length > 0) {
          setPendingDrop({
            itemId,
            newStatus: targetColumn,
            blockers: blockersDto.blockers,
          });
          return;
        }
      } catch {
        // If blockers fetch fails, proceed with status change anyway
      }
    }

    await executeStatusChange(itemId, targetColumn);
  }

  const executeStatusChange = useCallback(
    async (itemId: string, newStatus: WorkItemStatus) => {
      try {
        await changeStatus(itemId, { status: newStatus });
        toast.success(`Moved to ${COLUMN_LABELS[newStatus] ?? newStatus}`);
        onBoardUpdate();
      } catch (err) {
        const message = err instanceof Error ? err.message : "Failed to update status";
        toast.error(message);
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [onBoardUpdate],
  );

  async function handleConfirmBlockedDrop() {
    if (!pendingDrop) return;
    const { itemId, newStatus } = pendingDrop;
    setPendingDrop(null);
    await executeStatusChange(itemId, newStatus);
  }

  function handleCancelBlockedDrop() {
    setPendingDrop(null);
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
    >
      {children}

      {/* Drag overlay — ghost card follows cursor */}
      <DragOverlay>
        {activeItem ? <KanbanCardGhost item={activeItem} /> : null}
      </DragOverlay>

      {/* Blocked item confirmation dialog */}
      {pendingDrop && (
        <ConfirmBlockedDialog
          blockers={pendingDrop.blockers}
          onConfirm={handleConfirmBlockedDrop}
          onCancel={handleCancelBlockedDrop}
        />
      )}
    </DndContext>
  );
}
