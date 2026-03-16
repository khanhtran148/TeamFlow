"use client";

import { AlertCircle } from "lucide-react";

interface ReleaseOverdueBadgeProps {
  isOverdue: boolean;
}

export function ReleaseOverdueBadge({ isOverdue }: ReleaseOverdueBadgeProps) {
  if (!isOverdue) return null;

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        padding: "2px 8px",
        borderRadius: 100,
        fontSize: 10,
        fontWeight: 600,
        background: "var(--tf-red-dim)",
        color: "var(--tf-red)",
        border: "1px solid var(--tf-red-dim)",
      }}
    >
      <AlertCircle size={10} />
      Overdue
    </span>
  );
}
