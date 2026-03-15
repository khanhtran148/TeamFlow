"use client";

import { UserAvatar } from "@/components/shared/user-avatar";
import type { WorkItemHistoryDto } from "@/lib/hooks/use-work-item-history";

const ACTION_COLORS: Record<string, string> = {
  Created: "var(--tf-green, #22c55e)",
  StatusChanged: "var(--tf-blue, #3b82f6)",
  Updated: "var(--tf-accent)",
  Assigned: "var(--tf-purple, #a855f7)",
  Unassigned: "var(--tf-text3)",
  Deleted: "#ef4444",
  Rejected: "#ef4444",
  Linked: "var(--tf-blue, #3b82f6)",
  Unlinked: "var(--tf-text3)",
  Moved: "var(--tf-accent)",
};

function formatRelativeTime(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffSec < 60) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  if (diffHour < 24) return `${diffHour}h ago`;
  if (diffDay < 30) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

function formatAction(entry: WorkItemHistoryDto): string {
  if (entry.fieldName && entry.oldValue && entry.newValue) {
    return `changed ${entry.fieldName} from "${entry.oldValue}" to "${entry.newValue}"`;
  }
  if (entry.fieldName && entry.newValue) {
    return `set ${entry.fieldName} to "${entry.newValue}"`;
  }
  return entry.actionType.replace(/([A-Z])/g, " $1").trim().toLowerCase();
}

interface HistoryEntryProps {
  entry: WorkItemHistoryDto;
}

export function HistoryEntry({ entry }: HistoryEntryProps) {
  const initials = entry.actorName
    ? entry.actorName
        .split(" ")
        .map((n) => n[0])
        .join("")
        .slice(0, 2)
        .toUpperCase()
    : entry.actorType === "System"
      ? "SY"
      : "AI";

  const actionColor = ACTION_COLORS[entry.actionType] ?? "var(--tf-text2)";

  return (
    <div
      style={{
        display: "flex",
        gap: 10,
        padding: "10px 0",
        borderBottom: "1px solid var(--tf-border)",
      }}
    >
      <UserAvatar initials={initials} size="sm" />
      <div style={{ flex: 1 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <span style={{ fontSize: 13, fontWeight: 500, color: "var(--tf-text)" }}>
            {entry.actorName ?? entry.actorType}
          </span>
          <span
            style={{
              fontSize: 11,
              padding: "1px 6px",
              borderRadius: 4,
              background: `${actionColor}20`,
              color: actionColor,
              fontWeight: 500,
            }}
          >
            {entry.actionType}
          </span>
          <span style={{ fontSize: 11, color: "var(--tf-text3)", marginLeft: "auto" }}>
            {formatRelativeTime(entry.createdAt)}
          </span>
        </div>
        <div style={{ fontSize: 12, color: "var(--tf-text2)", marginTop: 3 }}>
          {formatAction(entry)}
        </div>
        {entry.actionType === "Rejected" && entry.newValue && (
          <div
            style={{
              marginTop: 6,
              padding: "6px 10px",
              borderRadius: 6,
              background: "rgba(239, 68, 68, 0.08)",
              border: "1px solid rgba(239, 68, 68, 0.2)",
              color: "#ef4444",
              fontSize: 12,
            }}
          >
            Reason: {entry.newValue}
          </div>
        )}
      </div>
    </div>
  );
}
