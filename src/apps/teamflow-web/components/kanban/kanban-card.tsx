"use client";

import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useRouter, useParams } from "next/navigation";
import { TypeIcon } from "@/components/shared/type-icon";
import { PriorityIcon } from "@/components/shared/priority-icon";
import { UserAvatar } from "@/components/shared/user-avatar";
import type { KanbanItemDto } from "@/lib/api/types";

interface KanbanCardProps {
  item: KanbanItemDto;
}

export function KanbanCard({ item }: KanbanCardProps) {
  const router = useRouter();
  const params = useParams();
  const projectId = params.projectId as string;

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: item.id });

  const initials = item.assigneeName
    ? item.assigneeName
        .split(" ")
        .map((n) => n[0])
        .join("")
        .slice(0, 2)
    : "";

  function handleClick() {
    if (!isDragging) {
      router.push(`/projects/${projectId}/work-items/${item.id}`);
    }
  }

  return (
    <div
      ref={setNodeRef}
      {...attributes}
      {...listeners}
      onClick={handleClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") handleClick();
      }}
      style={{
        transform: CSS.Transform.toString(transform),
        transition: transition ?? "transform 0.2s cubic-bezier(0.4,0,0.2,1), border-color 0.2s, box-shadow 0.2s",
        opacity: isDragging ? 0.4 : 1,
        background: "var(--tf-bg3)",
        border: `1px solid ${item.isBlocked ? "var(--tf-red)" : "var(--tf-border)"}`,
        borderRadius: "var(--tf-radius)",
        padding: "10px 12px",
        cursor: isDragging ? "grabbing" : "grab",
        display: "flex",
        flexDirection: "column",
        gap: 8,
        userSelect: "none",
        animation: "fadeUp 0.2s ease backwards",
      }}
      onMouseEnter={(e) => {
        if (isDragging) return;
        const el = e.currentTarget as HTMLDivElement;
        el.style.borderColor = item.isBlocked ? "var(--tf-red)" : "var(--tf-border2)";
        el.style.boxShadow = "var(--tf-shadow)";
        el.style.transform = "translateY(-1px)";
      }}
      onMouseLeave={(e) => {
        if (isDragging) return;
        const el = e.currentTarget as HTMLDivElement;
        el.style.borderColor = item.isBlocked ? "var(--tf-red)" : "var(--tf-border)";
        el.style.boxShadow = "none";
        el.style.transform = "";
      }}
    >
      {/* Top row: type icon + title + priority */}
      <div
        style={{
          display: "flex",
          alignItems: "flex-start",
          gap: 7,
        }}
      >
        <div style={{ marginTop: 1, flexShrink: 0 }}>
          <TypeIcon type={item.type} size={16} />
        </div>
        <span
          style={{
            flex: 1,
            fontSize: 12,
            fontWeight: 500,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-body)",
            lineHeight: 1.4,
            wordBreak: "break-word",
          }}
        >
          {item.title}
        </span>
        <PriorityIcon priority={item.priority} />
      </div>

      {/* Parent title (for non-epic items) */}
      {item.parentTitle && item.type !== "Epic" && (
        <div
          style={{
            fontSize: 10,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-body)",
            paddingLeft: 23,
            whiteSpace: "nowrap",
            overflow: "hidden",
            textOverflow: "ellipsis",
          }}
        >
          {item.parentTitle}
        </div>
      )}

      {/* Bottom row: assignee + blocked icon + release badge */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          paddingLeft: 23,
        }}
      >
        {item.assigneeName && (
          <UserAvatar
            initials={initials}
            name={item.assigneeName}
            size="xs"
          />
        )}

        <div style={{ flex: 1 }} />

        {item.releaseId && (
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              padding: "1px 6px",
              borderRadius: 100,
              fontSize: 9,
              fontWeight: 600,
              fontFamily: "var(--tf-font-mono)",
              background: "var(--tf-violet-dim)",
              color: "var(--tf-violet)",
              border: "1px solid var(--tf-violet-dim)",
              whiteSpace: "nowrap",
            }}
          >
            Release
          </span>
        )}

        {item.isBlocked && (
          <span
            title="Blocked"
            style={{
              width: 14,
              height: 14,
              borderRadius: "50%",
              background: "var(--tf-red-dim)",
              border: "1px solid var(--tf-red)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 8,
              color: "var(--tf-red)",
              flexShrink: 0,
              fontWeight: 700,
            }}
          >
            !
          </span>
        )}
      </div>
    </div>
  );
}

// Ghost card used in the drag overlay — no dnd-kit hooks
export function KanbanCardGhost({ item }: { item: KanbanItemDto }) {
  const initials = item.assigneeName
    ? item.assigneeName
        .split(" ")
        .map((n) => n[0])
        .join("")
        .slice(0, 2)
    : "";

  return (
    <div
      style={{
        background: "var(--tf-bg3)",
        border: `1px solid var(--tf-accent)`,
        borderRadius: "var(--tf-radius)",
        padding: "10px 12px",
        display: "flex",
        flexDirection: "column",
        gap: 8,
        boxShadow: "var(--tf-shadow)",
        opacity: 0.95,
        cursor: "grabbing",
        pointerEvents: "none",
        minWidth: 200,
      }}
    >
      <div style={{ display: "flex", alignItems: "flex-start", gap: 7 }}>
        <div style={{ marginTop: 1, flexShrink: 0 }}>
          <TypeIcon type={item.type} size={16} />
        </div>
        <span
          style={{
            flex: 1,
            fontSize: 12,
            fontWeight: 500,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-body)",
            lineHeight: 1.4,
          }}
        >
          {item.title}
        </span>
        <PriorityIcon priority={item.priority} />
      </div>
      {item.assigneeName && (
        <div style={{ paddingLeft: 23 }}>
          <UserAvatar initials={initials} name={item.assigneeName} size="xs" />
        </div>
      )}
    </div>
  );
}
