"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useBulkUpdatePriority } from "@/lib/hooks/use-backlog";
import type { Priority } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

const PRIORITY_OPTIONS: { value: Priority; label: string; color: string }[] = [
  { value: "Critical", label: "Critical", color: "var(--tf-red)" },
  { value: "High", label: "High", color: "var(--tf-orange)" },
  { value: "Medium", label: "Medium", color: "var(--tf-yellow)" },
  { value: "Low", label: "Low", color: "var(--tf-text3)" },
];

interface BulkPriorityDialogProps {
  open: boolean;
  projectId: string;
  selectedItemIds: string[];
  onClose: () => void;
}

export function BulkPriorityDialog({
  open,
  projectId,
  selectedItemIds,
  onClose,
}: BulkPriorityDialogProps) {
  const [priority, setPriority] = useState<Priority>("Medium");
  const mutation = useBulkUpdatePriority(projectId);

  if (!open) return null;

  async function handleSubmit() {
    try {
      await mutation.mutateAsync({
        items: selectedItemIds.map((id) => ({
          workItemId: id,
          priority,
        })),
      });
      toast.success(
        `Updated priority to ${priority} for ${selectedItemIds.length} items`,
      );
      onClose();
    } catch (err) {
      toast.error(
        (err as ApiError).message ?? "Failed to update priorities",
      );
    }
  }

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
      onClick={onClose}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 12,
          padding: 24,
          width: 340,
          display: "flex",
          flexDirection: "column",
          gap: 16,
        }}
      >
        <h3
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 16,
            color: "var(--tf-text)",
            margin: 0,
          }}
        >
          Bulk Update Priority
        </h3>
        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            margin: 0,
          }}
        >
          Set priority for {selectedItemIds.length} selected item
          {selectedItemIds.length !== 1 ? "s" : ""}.
        </p>

        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {PRIORITY_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              onClick={() => setPriority(opt.value)}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                padding: "10px 12px",
                borderRadius: 8,
                border: `1px solid ${priority === opt.value ? opt.color : "var(--tf-border)"}`,
                background:
                  priority === opt.value
                    ? "var(--tf-bg3)"
                    : "transparent",
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                fontSize: 13,
                color: "var(--tf-text)",
                fontWeight: priority === opt.value ? 600 : 400,
                transition: "all var(--tf-tr)",
              }}
            >
              <span
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: opt.color,
                  flexShrink: 0,
                }}
              />
              {opt.label}
            </button>
          ))}
        </div>

        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button
            onClick={onClose}
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
            onClick={handleSubmit}
            disabled={mutation.isPending}
            style={{
              padding: "7px 16px",
              borderRadius: 6,
              border: "none",
              background: "var(--tf-accent)",
              color: "var(--tf-bg)",
              fontSize: 13,
              fontWeight: 600,
              cursor: mutation.isPending ? "not-allowed" : "pointer",
              fontFamily: "var(--tf-font-body)",
              opacity: mutation.isPending ? 0.6 : 1,
            }}
          >
            {mutation.isPending ? "Updating..." : "Update Priority"}
          </button>
        </div>
      </div>
    </div>
  );
}
