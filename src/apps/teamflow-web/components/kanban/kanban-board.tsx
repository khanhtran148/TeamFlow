"use client";

import { KanbanColumn } from "./kanban-column";
import { KanbanDndProvider } from "./kanban-dnd-provider";
import type { KanbanBoardDto } from "@/lib/api/types";

interface KanbanBoardProps {
  board: KanbanBoardDto;
  onRefresh: () => void;
}

export function KanbanBoard({ board, onRefresh }: KanbanBoardProps) {
  return (
    <KanbanDndProvider board={board} onBoardUpdate={onRefresh}>
      <div
        className="kanban-board-scroll"
        style={{
          display: "flex",
          gap: 12,
          padding: "16px 20px",
          minWidth: "min-content",
          flex: 1,
          alignItems: "flex-start",
        }}
      >
        {board.columns.map((column) => (
          <KanbanColumn key={column.status} column={column} />
        ))}
      </div>
    </KanbanDndProvider>
  );
}
