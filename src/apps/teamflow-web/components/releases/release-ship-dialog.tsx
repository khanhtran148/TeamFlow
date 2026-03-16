"use client";

import { AlertTriangle, Rocket } from "lucide-react";
import { StatusBadge } from "@/components/shared/status-badge";
import type { IncompleteItemDto } from "@/lib/api/types";

interface ReleaseShipDialogProps {
  open: boolean;
  incompleteItems: IncompleteItemDto[];
  isPending: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ReleaseShipDialog({
  open,
  incompleteItems,
  isPending,
  onConfirm,
  onCancel,
}: ReleaseShipDialogProps) {
  if (!open) return null;

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.5)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 100,
      }}
      onClick={onCancel}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 12,
          padding: 24,
          width: 440,
          maxHeight: "80vh",
          overflow: "auto",
          display: "flex",
          flexDirection: "column",
          gap: 16,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
          }}
        >
          <AlertTriangle size={18} style={{ color: "var(--tf-orange)" }} />
          <h3
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 16,
              color: "var(--tf-text)",
              margin: 0,
            }}
          >
            Ship with Open Items?
          </h3>
        </div>

        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            margin: 0,
            lineHeight: 1.5,
          }}
        >
          This release has {incompleteItems.length} incomplete work item
          {incompleteItems.length !== 1 ? "s" : ""}. Are you sure you want to
          ship?
        </p>

        {/* Incomplete items list */}
        <div
          style={{
            background: "var(--tf-bg4)",
            borderRadius: 6,
            border: "1px solid var(--tf-border)",
            maxHeight: 200,
            overflow: "auto",
          }}
        >
          {incompleteItems.map((item) => (
            <div
              key={item.id}
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                padding: "8px 12px",
                borderBottom: "1px solid var(--tf-border)",
                fontSize: 13,
              }}
            >
              <span
                style={{
                  color: "var(--tf-text)",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                  flex: 1,
                  marginRight: 8,
                }}
              >
                {item.title}
              </span>
              <StatusBadge status={item.status} size="sm" />
            </div>
          ))}
        </div>

        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button
            onClick={onCancel}
            style={{
              padding: "7px 16px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 13,
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            disabled={isPending}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              padding: "7px 16px",
              borderRadius: 6,
              border: "none",
              background: "var(--tf-orange)",
              color: "#fff",
              fontSize: 13,
              fontWeight: 600,
              cursor: isPending ? "not-allowed" : "pointer",
              fontFamily: "var(--tf-font-body)",
              opacity: isPending ? 0.6 : 1,
            }}
          >
            <Rocket size={12} />
            {isPending ? "Shipping..." : "Ship Anyway"}
          </button>
        </div>
      </div>
    </div>
  );
}
