"use client";

import { AlertCircle } from "lucide-react";
import { usePreviousActionItems } from "@/lib/hooks/use-retros";
import type { RetroActionItemDto } from "@/lib/api/types";

interface RetroPreviousActionsProps {
  projectId: string;
}

export function RetroPreviousActions({ projectId }: RetroPreviousActionsProps) {
  const { data: items, isLoading } = usePreviousActionItems(projectId);

  if (isLoading || !items || items.length === 0) return null;

  return (
    <div
      style={{
        background: "var(--tf-orange-dim)",
        border: "1px solid var(--tf-orange)",
        borderRadius: 8,
        padding: "12px 16px",
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          marginBottom: 8,
          fontSize: 12,
          fontWeight: 600,
          color: "var(--tf-orange)",
        }}
      >
        <AlertCircle size={13} />
        Previous Action Items ({items.length})
      </div>
      <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
        {items.map((item) => (
          <div
            key={item.id}
            style={{
              fontSize: 12,
              color: "var(--tf-text)",
              display: "flex",
              alignItems: "center",
              gap: 6,
            }}
          >
            <span style={{ color: "var(--tf-orange)" }}>--</span>
            {item.title}
            {item.assigneeName && (
              <span style={{ color: "var(--tf-text3)", fontSize: 11 }}>
                ({item.assigneeName})
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
