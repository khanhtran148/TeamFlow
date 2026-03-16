"use client";

import { useMemo } from "react";
import { arrayMove } from "@dnd-kit/sortable";
import { LayoutList } from "lucide-react";
import { EpicGroup } from "./epic-group";
import { BacklogRow } from "./backlog-row";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { EmptyState } from "@/components/shared/empty-state";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import type { BacklogItemDto, BlockerItemDto } from "@/lib/api/types";

interface BacklogListProps {
  items: BacklogItemDto[];
  blockersMap: Record<string, BlockerItemDto[]>;
  viewMode: "grouped" | "flat";
  isLoading: boolean;
  onReorder: (reorderedItems: BacklogItemDto[]) => void;
}

interface SortableFlatRowProps {
  item: BacklogItemDto;
  blockers?: BlockerItemDto[];
}

function SortableFlatRow({ item, blockers }: SortableFlatRowProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: item.id });

  return (
    <div
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
      }}
    >
      <BacklogRow
        item={item}
        blockers={blockers}
        dragHandleProps={{ ...attributes, ...listeners }}
        isDragging={isDragging}
      />
    </div>
  );
}

export function BacklogList({
  items,
  blockersMap,
  viewMode,
  isLoading,
  onReorder,
}: BacklogListProps) {
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  // Build epic groups: group non-epic items by their parent epic
  const { epicGroups, ungroupedEpics } = useMemo(() => {
    const epics = items.filter((item) => item.type === "Epic");
    const nonEpics = items.filter((item) => item.type !== "Epic");

    // Map parent epic -> children
    const epicMap = new Map<string | null, BacklogItemDto[]>();
    epicMap.set(null, []); // No-epic group

    for (const epic of epics) {
      epicMap.set(epic.id, []);
    }

    for (const item of nonEpics) {
      const key = item.parentId ?? null;
      // Check if parent is in our epic list
      const parentIsEpic = epics.some((e) => e.id === key);
      if (parentIsEpic && key !== null) {
        epicMap.get(key)!.push(item);
      } else {
        epicMap.get(null)!.push(item);
      }
    }

    return {
      epicGroups: epicMap,
      ungroupedEpics: epics,
    };
  }, [items]);

  const flatItemIds = useMemo(() => items.map((i) => i.id), [items]);

  function handleFlatDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = items.findIndex((item) => item.id === active.id);
    const newIndex = items.findIndex((item) => item.id === over.id);
    if (oldIndex !== -1 && newIndex !== -1) {
      onReorder(arrayMove(items, oldIndex, newIndex));
    }
  }

  if (isLoading) {
    return (
      <div style={{ padding: 20 }}>
        <LoadingSkeleton rows={8} type="list-row" />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div style={{ padding: 40 }}>
        <EmptyState
          icon={LayoutList}
          title="No items in backlog"
          description="Create your first work item to get started."
        />
      </div>
    );
  }

  if (viewMode === "flat") {
    return (
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragEnd={handleFlatDragEnd}
      >
        <SortableContext
          items={flatItemIds}
          strategy={verticalListSortingStrategy}
        >
          <div>
            {items.map((item) => (
              <SortableFlatRow
                key={item.id}
                item={item}
                blockers={blockersMap[item.id]}
              />
            ))}
          </div>
        </SortableContext>
      </DndContext>
    );
  }

  // Grouped mode: render epics with their children below them
  // For DnD in grouped mode, we treat all items as a flat sortable list
  // but render them grouped
  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragEnd={handleFlatDragEnd}
    >
      <SortableContext
        items={flatItemIds}
        strategy={verticalListSortingStrategy}
      >
        <div>
          {ungroupedEpics.map((epic) => (
            <EpicGroup
              key={epic.id}
              epic={epic}
              items={epicGroups.get(epic.id) ?? []}
              blockersMap={blockersMap}
            />
          ))}
          {/* No-epic group */}
          {(epicGroups.get(null)?.length ?? 0) > 0 && (
            <EpicGroup
              key="no-epic"
              epic={null}
              items={epicGroups.get(null) ?? []}
              blockersMap={blockersMap}
            />
          )}
        </div>
      </SortableContext>
    </DndContext>
  );
}
