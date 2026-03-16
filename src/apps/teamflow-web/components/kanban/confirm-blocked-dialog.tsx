"use client";

import type { BlockerItemDto } from "@/lib/api/types";

interface ConfirmBlockedDialogProps {
  blockers: BlockerItemDto[];
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmBlockedDialog({
  blockers,
  onConfirm,
  onCancel,
}: ConfirmBlockedDialogProps) {
  return (
    /* Backdrop */
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="blocked-dialog-title"
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(0,0,0,0.6)",
      }}
      onClick={onCancel}
    >
      {/* Panel */}
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg3)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          padding: "24px",
          minWidth: 360,
          maxWidth: 480,
          width: "90vw",
          boxShadow: "var(--tf-shadow)",
        }}
      >
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 10,
            marginBottom: 12,
          }}
        >
          <span
            style={{
              width: 28,
              height: 28,
              borderRadius: "50%",
              background: "var(--tf-red-dim)",
              border: "1px solid var(--tf-red)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 14,
              flexShrink: 0,
            }}
          >
            ⚠
          </span>
          <span
            id="blocked-dialog-title"
            style={{
              fontSize: 14,
              fontWeight: 600,
              color: "var(--tf-text)",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            Item has unresolved blockers
          </span>
        </div>

        {/* Description */}
        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text2)",
            fontFamily: "var(--tf-font-body)",
            marginBottom: 16,
            lineHeight: 1.6,
          }}
        >
          This item is blocked by the following items that are not yet done.
          Are you sure you want to move it to{" "}
          <strong style={{ color: "var(--tf-blue)" }}>In Progress</strong>?
        </p>

        {/* Blocker list */}
        <ul
          style={{
            listStyle: "none",
            margin: 0,
            padding: 0,
            marginBottom: 20,
            display: "flex",
            flexDirection: "column",
            gap: 6,
          }}
        >
          {blockers.map((b) => (
            <li
              key={b.blockerId}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                padding: "6px 10px",
                background: "var(--tf-bg4)",
                border: "1px solid var(--tf-border)",
                borderRadius: 6,
                fontSize: 13,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              <span
                style={{
                  width: 6,
                  height: 6,
                  borderRadius: "50%",
                  background: "var(--tf-red)",
                  flexShrink: 0,
                }}
              />
              <span style={{ color: "var(--tf-text)", flex: 1 }}>{b.title}</span>
              <span
                style={{
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {b.status}
              </span>
            </li>
          ))}
        </ul>

        {/* Actions */}
        <div
          style={{
            display: "flex",
            justifyContent: "flex-end",
            gap: 8,
          }}
        >
          <button
            onClick={onCancel}
            style={{
              padding: "6px 16px",
              borderRadius: 6,
              fontSize: 13,
              fontWeight: 500,
              fontFamily: "var(--tf-font-body)",
              background: "transparent",
              border: "1px solid var(--tf-border)",
              color: "var(--tf-text2)",
              cursor: "pointer",
              transition: "all var(--tf-tr)",
            }}
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            style={{
              padding: "6px 16px",
              borderRadius: 6,
              fontSize: 13,
              fontWeight: 600,
              fontFamily: "var(--tf-font-body)",
              background: "var(--tf-red)",
              border: "none",
              color: "white",
              cursor: "pointer",
              transition: "all var(--tf-tr)",
            }}
          >
            Move Anyway
          </button>
        </div>
      </div>
    </div>
  );
}
