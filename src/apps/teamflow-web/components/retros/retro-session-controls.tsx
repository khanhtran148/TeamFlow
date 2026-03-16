"use client";

import { Play, ArrowRight, X } from "lucide-react";
import type { RetroSessionStatus } from "@/lib/api/types";

interface RetroSessionControlsProps {
  status: RetroSessionStatus;
  canFacilitate: boolean;
  onStart: () => void;
  onTransition: (target: RetroSessionStatus) => void;
  onClose: () => void;
  isPending: boolean;
}

export function RetroSessionControls({
  status,
  canFacilitate,
  onStart,
  onTransition,
  onClose,
  isPending,
}: RetroSessionControlsProps) {
  if (!canFacilitate) return null;

  return (
    <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
      {status === "Draft" && (
        <button
          onClick={onStart}
          disabled={isPending}
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
            cursor: isPending ? "not-allowed" : "pointer",
            fontFamily: "var(--tf-font-body)",
            opacity: isPending ? 0.6 : 1,
          }}
        >
          <Play size={12} />
          Start Session
        </button>
      )}

      {status === "Open" && (
        <button
          onClick={() => onTransition("Voting")}
          disabled={isPending}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 5,
            padding: "7px 16px",
            borderRadius: 6,
            fontSize: 13,
            fontWeight: 600,
            border: "none",
            background: "var(--tf-blue)",
            color: "#fff",
            cursor: isPending ? "not-allowed" : "pointer",
            fontFamily: "var(--tf-font-body)",
            opacity: isPending ? 0.6 : 1,
          }}
        >
          <ArrowRight size={12} />
          Begin Voting
        </button>
      )}

      {status === "Voting" && (
        <button
          onClick={() => onTransition("Discussing")}
          disabled={isPending}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 5,
            padding: "7px 16px",
            borderRadius: 6,
            fontSize: 13,
            fontWeight: 600,
            border: "none",
            background: "var(--tf-orange)",
            color: "#fff",
            cursor: isPending ? "not-allowed" : "pointer",
            fontFamily: "var(--tf-font-body)",
            opacity: isPending ? 0.6 : 1,
          }}
        >
          <ArrowRight size={12} />
          Begin Discussion
        </button>
      )}

      {status === "Discussing" && (
        <button
          onClick={onClose}
          disabled={isPending}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 5,
            padding: "7px 16px",
            borderRadius: 6,
            fontSize: 13,
            fontWeight: 600,
            border: "none",
            background: "var(--tf-red)",
            color: "#fff",
            cursor: isPending ? "not-allowed" : "pointer",
            fontFamily: "var(--tf-font-body)",
            opacity: isPending ? 0.6 : 1,
          }}
        >
          <X size={12} />
          Close Session
        </button>
      )}
    </div>
  );
}
