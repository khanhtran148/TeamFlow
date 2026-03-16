"use client";

import { useState } from "react";
import { ApiError } from "@/lib/api/client";

interface UserStatusToggleProps {
  userId: string;
  userName: string;
  isActive: boolean;
  onToggle: (userId: string, isActive: boolean) => Promise<void>;
}

export function UserStatusToggle({
  userId,
  userName,
  isActive,
  onToggle,
}: UserStatusToggleProps) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleToggle() {
    setError(null);
    setLoading(true);
    try {
      await onToggle(userId, !isActive);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.problem.detail ?? err.problem.title);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to update status.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ position: "relative", display: "inline-flex" }}>
      <button
        type="button"
        onClick={handleToggle}
        disabled={loading}
        aria-label={`${isActive ? "Deactivate" : "Activate"} ${userName}`}
        title={`${isActive ? "Deactivate" : "Activate"} ${userName}`}
        style={{
          padding: "3px 10px",
          borderRadius: 100,
          border: isActive
            ? "1px solid rgba(110, 231, 183, 0.3)"
            : "1px solid rgba(248, 113, 113, 0.3)",
          background: isActive
            ? "rgba(110, 231, 183, 0.08)"
            : "rgba(248, 113, 113, 0.08)",
          color: isActive ? "var(--tf-accent)" : "var(--tf-red)",
          fontSize: 11,
          fontWeight: 600,
          fontFamily: "var(--tf-font-mono)",
          cursor: loading ? "not-allowed" : "pointer",
          opacity: loading ? 0.6 : 1,
          transition: "opacity 0.15s",
          minHeight: 24,
          minWidth: 72,
        }}
      >
        {loading ? "..." : isActive ? "Active" : "Inactive"}
      </button>
      {error && (
        <div
          role="alert"
          style={{
            position: "absolute",
            top: "100%",
            left: 0,
            marginTop: 4,
            padding: "4px 8px",
            borderRadius: 4,
            background: "var(--tf-bg3)",
            border: "1px solid rgba(248, 113, 113, 0.3)",
            color: "var(--tf-red)",
            fontSize: 11,
            fontFamily: "var(--tf-font-body)",
            whiteSpace: "nowrap",
            zIndex: 10,
          }}
        >
          {error}
        </div>
      )}
    </div>
  );
}
