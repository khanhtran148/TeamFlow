"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { BacklogRow } from "./backlog-row";
import type { BacklogItemDto, BlockerItemDto } from "@/lib/api/types";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

interface EpicGroupProps {
  epic: BacklogItemDto | null; // null = "No Epic" group
  items: BacklogItemDto[];
  blockersMap: Record<string, BlockerItemDto[]>;
  defaultExpanded?: boolean;
}

interface SortableBacklogRowProps {
  item: BacklogItemDto;
  blockers?: BlockerItemDto[];
}

function SortableBacklogRow({ item, blockers }: SortableBacklogRowProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: item.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div ref={setNodeRef} style={style}>
      <BacklogRow
        item={item}
        blockers={blockers}
        dragHandleProps={{ ...attributes, ...listeners }}
        isDragging={isDragging}
      />
    </div>
  );
}

const EPIC_COLORS = [
  "var(--tf-orange)",
  "var(--tf-blue)",
  "var(--tf-violet)",
  "var(--tf-accent)",
  "var(--tf-yellow)",
  "var(--tf-red)",
];

function getEpicColor(epicId: string | null): string {
  if (!epicId) return "var(--tf-text3)";
  let hash = 0;
  for (let i = 0; i < epicId.length; i++) {
    hash = epicId.charCodeAt(i) + ((hash << 5) - hash);
  }
  return EPIC_COLORS[Math.abs(hash) % EPIC_COLORS.length];
}

export function EpicGroup({
  epic,
  items,
  blockersMap,
  defaultExpanded = true,
}: EpicGroupProps) {
  const [expanded, setExpanded] = useState(defaultExpanded);

  const epicColor = getEpicColor(epic?.id ?? null);
  const totalPoints = items.reduce(
    (sum, item) => sum + (item.estimationValue ?? 0),
    0,
  );

  return (
    <div
      style={{
        marginBottom: 2,
      }}
    >
      {/* Epic header */}
      <div
        onClick={() => setExpanded(!expanded)}
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "6px 12px",
          cursor: "pointer",
          background: "var(--tf-bg3)",
          borderBottom: "1px solid var(--tf-border)",
          userSelect: "none",
        }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLDivElement).style.background =
            "var(--tf-bg4)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLDivElement).style.background =
            "var(--tf-bg3)";
        }}
      >
        {/* Expand/collapse icon */}
        <span style={{ color: "var(--tf-text3)", display: "flex" }}>
          {expanded ? (
            <ChevronDown size={14} />
          ) : (
            <ChevronRight size={14} />
          )}
        </span>

        {/* Epic color dot */}
        <span
          style={{
            width: 8,
            height: 8,
            borderRadius: "50%",
            background: epicColor,
            flexShrink: 0,
          }}
        />

        {/* Epic title */}
        <span
          style={{
            fontSize: 13,
            fontWeight: 600,
            color: epic ? epicColor : "var(--tf-text3)",
            fontFamily: "var(--tf-font-body)",
            flex: 1,
          }}
        >
          {epic ? epic.title : "No Epic"}
        </span>

        {/* Item count */}
        <span
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
            marginRight: 8,
          }}
        >
          {items.length} {items.length === 1 ? "item" : "items"}
        </span>

        {/* Total points */}
        {totalPoints > 0 && (
          <span
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {totalPoints}pt
          </span>
        )}
      </div>

      {/* Items */}
      {expanded && (
        <div style={{ paddingLeft: 16 }}>
          {items.length === 0 ? (
            <div
              style={{
                padding: "10px 12px",
                fontSize: 13,
                color: "var(--tf-text3)",
                fontFamily: "var(--tf-font-body)",
                fontStyle: "italic",
              }}
            >
              No items in this epic
            </div>
          ) : (
            items.map((item) => (
              <SortableBacklogRow
                key={item.id}
                item={item}
                blockers={blockersMap[item.id]}
              />
            ))
          )}
        </div>
      )}
    </div>
  );
}
