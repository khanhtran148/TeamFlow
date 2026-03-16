"use client";

import { Trophy } from "lucide-react";

interface PokerResultProps {
  finalEstimate: number;
}

export function PokerResult({ finalEstimate }: PokerResultProps) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "10px 14px",
        background: "var(--tf-accent-dim2)",
        border: "1px solid var(--tf-accent)",
        borderRadius: 8,
      }}
    >
      <Trophy size={15} style={{ color: "var(--tf-accent)" }} />
      <span
        style={{
          fontSize: 13,
          fontWeight: 600,
          color: "var(--tf-accent)",
        }}
      >
        Estimated: {finalEstimate} point{finalEstimate !== 1 ? "s" : ""}
      </span>
    </div>
  );
}
