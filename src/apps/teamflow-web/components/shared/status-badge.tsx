import type { WorkItemStatus } from "@/lib/api/types";

interface StatusBadgeProps {
  status: WorkItemStatus;
  size?: "sm" | "md";
}

const STATUS_CONFIG: Record<
  WorkItemStatus,
  { label: string; bg: string; color: string; border: string }
> = {
  ToDo: {
    label: "To Do",
    bg: "var(--tf-bg4)",
    color: "var(--tf-text2)",
    border: "var(--tf-border)",
  },
  InProgress: {
    label: "In Progress",
    bg: "var(--tf-blue-dim)",
    color: "var(--tf-blue)",
    border: "var(--tf-blue-dim)",
  },
  InReview: {
    label: "In Review",
    bg: "var(--tf-violet-dim)",
    color: "var(--tf-violet)",
    border: "var(--tf-violet-dim)",
  },
  NeedsClarification: {
    label: "Needs Clarification",
    bg: "var(--tf-yellow-dim)",
    color: "var(--tf-yellow)",
    border: "var(--tf-yellow-dim)",
  },
  Done: {
    label: "Done",
    bg: "var(--tf-accent-dim2)",
    color: "var(--tf-accent)",
    border: "var(--tf-accent-dim2)",
  },
  Rejected: {
    label: "Rejected",
    bg: "var(--tf-red-dim)",
    color: "var(--tf-red)",
    border: "var(--tf-red-dim)",
  },
};

export function StatusBadge({ status, size = "sm" }: StatusBadgeProps) {
  const config = STATUS_CONFIG[status];
  const fontSize = size === "sm" ? 9 : 11;
  const padding = size === "sm" ? "1px 7px" : "3px 10px";

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding,
        borderRadius: 100,
        fontSize,
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
