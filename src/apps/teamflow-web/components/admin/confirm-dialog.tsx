"use client";

import { AlertTriangle } from "lucide-react";

interface ConfirmDialogProps {
  title: string;
  message: string;
  confirmLabel?: string;
  confirmVariant?: "danger" | "default";
  onConfirm: () => void;
  onCancel: () => void;
  loading?: boolean;
}

export function ConfirmDialog({
  title,
  message,
  confirmLabel = "Confirm",
  confirmVariant = "default",
  onConfirm,
  onCancel,
  loading = false,
}: ConfirmDialogProps) {
  const isDanger = confirmVariant === "danger";

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
      }}
    >
      {/* Backdrop */}
      <div
        onClick={loading ? undefined : onCancel}
        aria-hidden="true"
        style={{
          position: "absolute",
          inset: 0,
          background: "rgba(0,0,0,0.6)",
          backdropFilter: "blur(2px)",
        }}
      />

      {/* Dialog */}
      <div
        style={{
          position: "relative",
          width: "100%",
          maxWidth: 400,
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 12,
          padding: 24,
          zIndex: 1,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 10,
            marginBottom: 16,
          }}
        >
          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: 8,
              background: isDanger
                ? "rgba(248, 113, 113, 0.1)"
                : "var(--tf-accent-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <AlertTriangle
              size={15}
              color={isDanger ? "var(--tf-red)" : "var(--tf-accent)"}
            />
          </div>
          <h2
            id="confirm-dialog-title"
            style={{
              fontSize: 15,
              fontWeight: 600,
              color: "var(--tf-text)",
              fontFamily: "var(--tf-font-head)",
              margin: 0,
            }}
          >
            {title}
          </h2>
        </div>

        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text2)",
            fontFamily: "var(--tf-font-body)",
            lineHeight: 1.5,
            margin: "0 0 20px 0",
          }}
        >
          {message}
        </p>

        <div style={{ display: "flex", gap: 8 }}>
          <button
            type="button"
            onClick={onCancel}
            disabled={loading}
            style={{
              flex: 1,
              padding: "8px 0",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 13,
              fontFamily: "var(--tf-font-body)",
              cursor: loading ? "not-allowed" : "pointer",
              minHeight: 36,
            }}
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={loading}
            style={{
              flex: 1,
              padding: "8px 0",
              borderRadius: 6,
              border: "none",
              background: isDanger
                ? "rgba(248, 113, 113, 0.9)"
                : "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
              color: isDanger ? "#fff" : "#0a0a0b",
              fontSize: 13,
              fontWeight: 600,
              fontFamily: "var(--tf-font-body)",
              cursor: loading ? "not-allowed" : "pointer",
              opacity: loading ? 0.7 : 1,
              minHeight: 36,
            }}
          >
            {loading ? "..." : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
