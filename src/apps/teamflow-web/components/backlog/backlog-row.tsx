"use client";

import { memo } from "react";
import { useRouter, useParams } from "next/navigation";
import { GripVertical, AlertCircle } from "lucide-react";
import { TypeIcon } from "@/components/shared/type-icon";
import { PriorityIcon } from "@/components/shared/priority-icon";
import { UserAvatar, formatAssignedAt } from "@/components/shared/user-avatar";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import type { BacklogItemDto, BlockerItemDto } from "@/lib/api/types";

interface BacklogRowProps {
  item: BacklogItemDto;
  blockers?: BlockerItemDto[];
  dragHandleProps?: Record<string, unknown>;
  isDragging?: boolean;
}

function ReleaseBadge({ name }: { name: string }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "1px 7px",
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
      {name}
    </span>
  );
}

function PointsBadge({ value }: { value: number }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "1px 6px",
        borderRadius: 4,
        fontSize: 13,
        fontWeight: 600,
        fontFamily: "var(--tf-font-mono)",
        background: "var(--tf-bg4)",
        color: "var(--tf-text3)",
        whiteSpace: "nowrap",
      }}
    >
      {value}pt
    </span>
  );
}

export const BacklogRow = memo(function BacklogRow({
  item,
  blockers,
  dragHandleProps,
  isDragging,
}: BacklogRowProps) {
  const router = useRouter();
  const params = useParams<{ projectId: string }>();
  const projectId = params?.projectId ?? "";

  const hasBlockers = item.isBlocked && blockers && blockers.length > 0;

  function handleRowClick() {
    router.push(`/projects/${projectId}/work-items/${item.id}`);
  }

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "6px 12px",
        borderBottom: "1px solid var(--tf-border)",
        background: isDragging ? "var(--tf-bg4)" : "transparent",
        cursor: "pointer",
        transition: "background var(--tf-tr)",
        opacity: isDragging ? 0.5 : 1,
      }}
      onMouseEnter={(e) => {
        if (!isDragging) {
          (e.currentTarget as HTMLDivElement).style.background =
            "var(--tf-bg3)";
        }
      }}
      onMouseLeave={(e) => {
        if (!isDragging) {
          (e.currentTarget as HTMLDivElement).style.background = "transparent";
        }
      }}
      onClick={handleRowClick}
    >
      {/* Drag handle */}
      <div
        {...dragHandleProps}
        onClick={(e) => e.stopPropagation()}
        style={{
          color: "var(--tf-text3)",
          cursor: "grab",
          display: "flex",
          alignItems: "center",
          flexShrink: 0,
          opacity: 0.5,
        }}
      >
        <GripVertical size={14} />
      </div>

      {/* Type icon */}
      <TypeIcon type={item.type} size={16} />

      {/* ID */}
      <span
        style={{
          fontFamily: "var(--tf-font-mono)",
          fontSize: 13,
          color: "var(--tf-text3)",
          minWidth: 60,
          flexShrink: 0,
        }}
      >
        #{item.id.slice(0, 8)}
      </span>

      {/* Title */}
      <span
        style={{
          flex: 1,
          fontSize: 13,
          color: "var(--tf-text)",
          fontFamily: "var(--tf-font-body)",
          overflow: "hidden",
          textOverflow: "ellipsis",
          whiteSpace: "nowrap",
        }}
      >
        {item.title}
      </span>

      {/* Blocked icon */}
      {hasBlockers && (
        <Tooltip>
          <TooltipTrigger
            onClick={(e) => e.stopPropagation()}
            style={{
              display: "flex",
              alignItems: "center",
              color: "var(--tf-red)",
              flexShrink: 0,
              cursor: "default",
              background: "none",
              border: "none",
              padding: 0,
            }}
          >
            <AlertCircle size={14} />
          </TooltipTrigger>
          <TooltipContent>
            <div
              style={{
                fontSize: 13,
                fontFamily: "var(--tf-font-body)",
                maxWidth: 240,
              }}
            >
              <div
                style={{
                  fontWeight: 600,
                  marginBottom: 4,
                  color: "var(--tf-red)",
                }}
              >
                Blocked by:
              </div>
              {blockers!.map((b) => (
                <div
                  key={b.blockerId}
                  style={{
                    color: "var(--tf-text2)",
                    marginBottom: 2,
                  }}
                >
                  • {b.title}
                </div>
              ))}
            </div>
          </TooltipContent>
        </Tooltip>
      )}

      {/* Priority */}
      <PriorityIcon priority={item.priority} />

      {/* Points — hidden on mobile */}
      {item.estimationValue != null && (
        <span className="backlog-col-hide-mobile">
          <PointsBadge value={item.estimationValue} />
        </span>
      )}

      {/* Release badge — hidden on mobile */}
      {item.releaseName && (
        <span className="backlog-col-hide-mobile">
          <ReleaseBadge name={item.releaseName} />
        </span>
      )}

      {/* Assignee */}
      {item.assigneeName ? (
        <UserAvatar
          initials={item.assigneeName
            .split(" ")
            .map((n) => n[0])
            .join("")
            .slice(0, 2)}
          name={item.assigneeName}
          subtitle={formatAssignedAt(item.assignedAt)}
          size="xs"
        />
      ) : (
        <div style={{ width: 18, flexShrink: 0 }} />
      )}
    </div>
  );
});
