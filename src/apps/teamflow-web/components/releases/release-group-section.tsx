"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import type { ReleaseGroupDto } from "@/lib/api/types";

interface ReleaseGroupSectionProps {
  group: ReleaseGroupDto;
}

export function ReleaseGroupSection({ group }: ReleaseGroupSectionProps) {
  const [expanded, setExpanded] = useState(true);
  const pct =
    group.itemCount > 0
      ? Math.round((group.doneCount / group.itemCount) * 100)
      : 0;

  return (
    <div
      style={{
        border: "1px solid var(--tf-border)",
        borderRadius: 6,
        overflow: "hidden",
      }}
    >
      <button
        onClick={() => setExpanded(!expanded)}
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          width: "100%",
          padding: "10px 12px",
          background: "var(--tf-bg3)",
          border: "none",
          cursor: "pointer",
          fontFamily: "var(--tf-font-body)",
          textAlign: "left",
        }}
      >
        {expanded ? (
          <ChevronDown size={12} style={{ color: "var(--tf-text3)" }} />
        ) : (
          <ChevronRight size={12} style={{ color: "var(--tf-text3)" }} />
        )}
        <span
          style={{
            flex: 1,
            fontSize: 13,
            fontWeight: 600,
            color: "var(--tf-text)",
          }}
        >
          {group.groupName}
        </span>
        <span
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {group.doneCount}/{group.itemCount} done ({pct}%)
        </span>
      </button>

      {expanded && (
        <div
          style={{
            padding: "8px 12px",
          }}
        >
          {/* Mini progress bar */}
          <div
            style={{
              height: 4,
              borderRadius: 100,
              background: "var(--tf-bg4)",
              overflow: "hidden",
            }}
          >
            <div
              style={{
                height: "100%",
                width: `${pct}%`,
                background: "var(--tf-accent)",
                borderRadius: 100,
                transition: "width 0.3s ease",
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
}
