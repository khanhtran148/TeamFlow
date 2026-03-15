"use client";

import { AlertTriangle } from "lucide-react";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  destructive?: boolean;
  isPending?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  "data-testid"?: string;
  confirmTestId?: string;
  cancelTestId?: string;
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = "Confirm",
  destructive = false,
  isPending = false,
  onConfirm,
  onCancel,
  "data-testid": testId,
  confirmTestId,
  cancelTestId,
}: ConfirmDialogProps) {
  if (!open) return null;

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 60,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(2px)",
      }}
      onClick={onCancel}
    >
      <div
        data-testid={testId}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          boxShadow: "var(--tf-shadow)",
          width: "100%",
          maxWidth: 380,
          padding: 20,
          display: "flex",
          flexDirection: "column",
          gap: 14,
        }}
      >
        {/* Icon + title */}
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: 8,
              background: destructive ? "var(--tf-red-dim)" : "var(--tf-yellow-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            <AlertTriangle
              size={15}
              color={destructive ? "var(--tf-red)" : "var(--tf-yellow)"}
            />
          </div>
          <h3
            id="confirm-dialog-title"
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 14,
              color: "var(--tf-text)",
            }}
          >
            {title}
          </h3>
        </div>

        {/* Message */}
        <p style={{ fontSize: 12, color: "var(--tf-text3)", lineHeight: 1.5 }}>
          {message}
        </p>

        {/* Actions */}
        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
          <button
            data-testid={cancelTestId}
            type="button"
            onClick={onCancel}
            disabled={isPending}
            style={{
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 12,
              fontWeight: 500,
              cursor: isPending ? "not-allowed" : "pointer",
              opacity: isPending ? 0.5 : 1,
            }}
          >
            Cancel
          </button>
          <button
            data-testid={confirmTestId}
            type="button"
            onClick={onConfirm}
            disabled={isPending}
            style={{
              padding: "7px 14px",
              borderRadius: 6,
              border: destructive ? "1px solid var(--tf-red)" : "1px solid var(--tf-yellow)",
              background: destructive ? "var(--tf-red-dim)" : "var(--tf-yellow-dim)",
              color: destructive ? "var(--tf-red)" : "var(--tf-yellow)",
              fontSize: 12,
              fontWeight: 600,
              cursor: isPending ? "not-allowed" : "pointer",
              opacity: isPending ? 0.7 : 1,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            {isPending ? "Processing..." : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
