"use client";

import { useDroppable } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { KanbanCard } from "./kanban-card";
import type { KanbanColumnDto } from "@/lib/api/types";

const COLUMN_CONFIG: Record<
  string,
  { label: string; accentColor: string; dotColor: string }
> = {
  ToDo: {
    label: "To Do",
    accentColor: "var(--tf-text3)",
    dotColor: "var(--tf-bg4)",
  },
  InProgress: {
    label: "In Progress",
    accentColor: "var(--tf-blue)",
    dotColor: "var(--tf-blue)",
  },
  InReview: {
    label: "In Review",
    accentColor: "var(--tf-violet)",
    dotColor: "var(--tf-violet)",
  },
  Done: {
    label: "Done",
    accentColor: "var(--tf-accent)",
    dotColor: "var(--tf-accent)",
  },
};

interface KanbanColumnProps {
  column: KanbanColumnDto;
}

export function KanbanColumn({ column }: KanbanColumnProps) {
  const config = COLUMN_CONFIG[column.status] ?? {
    label: column.status,
    accentColor: "var(--tf-text3)",
    dotColor: "var(--tf-text3)",
  };

  const { setNodeRef, isOver } = useDroppable({ id: column.status });

  return (
    <div
      style={{
        flex: "1 1 0",
        minWidth: 220,
        maxWidth: 320,
        display: "flex",
        flexDirection: "column",
        gap: 0,
      }}
    >
      {/* Column header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "10px 12px",
          borderBottom: `2px solid ${config.accentColor}`,
          marginBottom: 8,
        }}
      >
        <span
          style={{
            width: 8,
            height: 8,
            borderRadius: "50%",
            background: config.dotColor,
            flexShrink: 0,
          }}
        />
        <span
          style={{
            fontSize: 11,
            fontWeight: 700,
            color: "var(--tf-text2)",
            fontFamily: "var(--tf-font-mono)",
            textTransform: "uppercase",
            letterSpacing: "0.06em",
            flex: 1,
          }}
        >
          {config.label}
        </span>
        <span
          style={{
            fontSize: 10,
            fontWeight: 600,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
            background: "var(--tf-bg4)",
            border: "1px solid var(--tf-border)",
            borderRadius: 100,
            padding: "1px 7px",
          }}
        >
          {column.itemCount}
        </span>
      </div>

      {/* Drop area */}
      <div
        ref={setNodeRef}
        style={{
          flex: 1,
          display: "flex",
          flexDirection: "column",
          gap: 6,
          padding: "4px 4px 12px",
          minHeight: 120,
          borderRadius: 6,
          background: isOver ? "var(--tf-accent-dim)" : "transparent",
          transition: "background var(--tf-tr)",
          border: isOver
            ? `1px dashed ${config.accentColor}`
            : "1px solid transparent",
        }}
      >
        <SortableContext
          items={column.items.map((item) => item.id)}
          strategy={verticalListSortingStrategy}
        >
          {column.items.map((item) => (
            <KanbanCard key={item.id} item={item} />
          ))}
        </SortableContext>

        {column.items.length === 0 && (
          <div
            style={{
              flex: 1,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 11,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-body)",
              fontStyle: "italic",
              padding: "20px 0",
            }}
          >
            No items
          </div>
        )}
      </div>
    </div>
  );
}
