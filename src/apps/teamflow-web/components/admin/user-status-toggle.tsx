"use client";

import { useState } from "react";
import { ApiError } from "@/lib/api/client";
import { ConfirmDialog } from "@/components/admin/confirm-dialog";

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
  const [showConfirm, setShowConfirm] = useState(false);

  async function handleConfirm() {
    setError(null);
    setLoading(true);
    try {
      await onToggle(userId, !isActive);
      setShowConfirm(false);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.problem.detail ?? err.problem.title);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to update status.");
      }
      setShowConfirm(false);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ position: "relative", display: "inline-flex" }}>
      <button
        type="button"
        onClick={() => setShowConfirm(true)}
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
      {showConfirm && (
        <ConfirmDialog
          title={isActive ? "Deactivate User" : "Activate User"}
          message={
            isActive
              ? `Are you sure you want to deactivate ${userName}? They will lose access until reactivated.`
              : `Are you sure you want to activate ${userName}?`
          }
          confirmLabel={isActive ? "Deactivate" : "Activate"}
          confirmVariant={isActive ? "danger" : "default"}
          onConfirm={handleConfirm}
          onCancel={() => setShowConfirm(false)}
          loading={loading}
        />
      )}
    </div>
  );
}
