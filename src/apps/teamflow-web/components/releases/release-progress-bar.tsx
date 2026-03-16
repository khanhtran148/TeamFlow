"use client";

import type { ReleaseProgressDto } from "@/lib/api/types";

interface ReleaseProgressBarProps {
  progress: ReleaseProgressDto;
}

export function ReleaseProgressBar({ progress }: ReleaseProgressBarProps) {
  const total = progress.totalItems;
  if (total === 0) {
    return (
      <div
        style={{
          fontSize: 12,
          color: "var(--tf-text3)",
          textAlign: "center",
          padding: 10,
        }}
      >
        No items in this release.
      </div>
    );
  }

  const donePct = Math.round((progress.doneItems / total) * 100);
  const inProgressPct = Math.round((progress.inProgressItems / total) * 100);
  const toDoPct = 100 - donePct - inProgressPct;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      {/* Stacked bar */}
      <div
        style={{
          height: 10,
          borderRadius: 100,
          overflow: "hidden",
          display: "flex",
          background: "var(--tf-bg4)",
        }}
      >
        {donePct > 0 && (
          <div
            style={{
              width: `${donePct}%`,
              background: "var(--tf-accent)",
              transition: "width 0.3s ease",
            }}
          />
        )}
        {inProgressPct > 0 && (
          <div
            style={{
              width: `${inProgressPct}%`,
              background: "var(--tf-blue)",
              transition: "width 0.3s ease",
            }}
          />
        )}
      </div>

      {/* Legend */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 16,
          fontSize: 11,
          color: "var(--tf-text3)",
          fontFamily: "var(--tf-font-mono)",
        }}
      >
        <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
          <span
            style={{
              width: 8,
              height: 8,
              borderRadius: "50%",
              background: "var(--tf-accent)",
            }}
          />
          Done: {progress.doneItems} ({donePct}%)
        </span>
        <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
          <span
            style={{
              width: 8,
              height: 8,
              borderRadius: "50%",
              background: "var(--tf-blue)",
            }}
          />
          In Progress: {progress.inProgressItems} ({inProgressPct}%)
        </span>
        <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
          <span
            style={{
              width: 8,
              height: 8,
              borderRadius: "50%",
              background: "var(--tf-bg4)",
              border: "1px solid var(--tf-border)",
            }}
          />
          To Do: {progress.toDoItems} ({toDoPct}%)
        </span>
      </div>

      {/* Points info */}
      {progress.totalPoints > 0 && (
        <div
          style={{
            fontSize: 11,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          Points: {progress.donePoints}/{progress.totalPoints} completed
        </div>
      )}
    </div>
  );
}
