"use client";

import type { SprintStatus } from "@/lib/api/types";

interface SprintStatusBadgeProps {
  status: SprintStatus;
}

const STATUS_CONFIG: Record<
  SprintStatus,
  { label: string; bg: string; color: string; border: string }
> = {
  Planning: {
    label: "Planning",
    bg: "var(--tf-blue-dim)",
    color: "var(--tf-blue)",
    border: "var(--tf-blue)",
  },
  Active: {
    label: "Active",
    bg: "var(--tf-accent-dim2)",
    color: "var(--tf-accent)",
    border: "var(--tf-accent)",
  },
  Completed: {
    label: "Completed",
    bg: "var(--tf-bg4)",
    color: "var(--tf-text3)",
    border: "var(--tf-border)",
  },
};

export function SprintStatusBadge({ status }: SprintStatusBadgeProps) {
  const config = STATUS_CONFIG[status];
  return (
    <span
      data-testid={`sprint-status-${status.toLowerCase()}`}
      role="status"
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "2px 8px",
        borderRadius: 100,
        fontSize: 13,
        fontWeight: 600,
        fontFamily: "var(--tf-font-mono)",
        background: config.bg,
        color: config.color,
        border: `1px solid ${config.border}`,
        whiteSpace: "nowrap",
      }}
    >
      {config.label}
    </span>
  );
}
