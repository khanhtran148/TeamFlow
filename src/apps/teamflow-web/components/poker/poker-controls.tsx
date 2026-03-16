"use client";

import { useState } from "react";
import { Eye, CheckCircle2 } from "lucide-react";

const FIBONACCI_VALUES = [1, 2, 3, 5, 8, 13, 21];

interface PokerControlsProps {
  canFacilitate: boolean;
  isRevealed: boolean;
  finalEstimate: number | null;
  onReveal: () => void;
  onConfirm: (value: number) => void;
  revealPending: boolean;
  confirmPending: boolean;
}

export function PokerControls({
  canFacilitate,
  isRevealed,
  finalEstimate,
  onReveal,
  onConfirm,
  revealPending,
  confirmPending,
}: PokerControlsProps) {
  const [selectedEstimate, setSelectedEstimate] = useState<number | null>(null);

  if (!canFacilitate) return null;

  // Already confirmed
  if (finalEstimate !== null) {
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
        <CheckCircle2 size={14} style={{ color: "var(--tf-accent)" }} />
        <span
          style={{
            fontSize: 13,
            fontWeight: 600,
            color: "var(--tf-accent)",
          }}
        >
          Final estimate: {finalEstimate} points
        </span>
      </div>
    );
  }

  return (
    <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
      {!isRevealed && (
        <button
          onClick={onReveal}
          disabled={revealPending}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 5,
            padding: "7px 16px",
            borderRadius: 6,
            fontSize: 13,
            fontWeight: 600,
            border: "none",
            background: "var(--tf-accent)",
            color: "var(--tf-bg)",
            cursor: revealPending ? "not-allowed" : "pointer",
            fontFamily: "var(--tf-font-body)",
            opacity: revealPending ? 0.6 : 1,
          }}
        >
          <Eye size={12} />
          {revealPending ? "Revealing..." : "Reveal Votes"}
        </button>
      )}

      {isRevealed && (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
          }}
        >
          <span
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
            }}
          >
            Confirm estimate:
          </span>
          {FIBONACCI_VALUES.map((val) => (
            <button
              key={val}
              onClick={() => setSelectedEstimate(val)}
              style={{
                width: 34,
                height: 34,
                borderRadius: 6,
                border: `1px solid ${selectedEstimate === val ? "var(--tf-accent)" : "var(--tf-border)"}`,
                background:
                  selectedEstimate === val
                    ? "var(--tf-accent-dim2)"
                    : "transparent",
                color:
                  selectedEstimate === val
                    ? "var(--tf-accent)"
                    : "var(--tf-text2)",
                fontSize: 13,
                fontWeight: 600,
                fontFamily: "var(--tf-font-mono)",
                cursor: "pointer",
                transition: "all var(--tf-tr)",
              }}
            >
              {val}
            </button>
          ))}
          {selectedEstimate !== null && (
            <button
              onClick={() => {
                if (selectedEstimate !== null) onConfirm(selectedEstimate);
              }}
              disabled={confirmPending}
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 5,
                padding: "7px 14px",
                borderRadius: 6,
                fontSize: 13,
                fontWeight: 600,
                border: "none",
                background: "var(--tf-accent)",
                color: "var(--tf-bg)",
                cursor: confirmPending ? "not-allowed" : "pointer",
                fontFamily: "var(--tf-font-body)",
              }}
            >
              <CheckCircle2 size={12} />
              Confirm
            </button>
          )}
        </div>
      )}
    </div>
  );
}
