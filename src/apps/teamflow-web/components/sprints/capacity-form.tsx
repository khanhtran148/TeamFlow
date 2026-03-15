"use client";

import { useState, useEffect } from "react";
import { X } from "lucide-react";
import { toast } from "sonner";
import { useUpdateSprintCapacity } from "@/lib/hooks/use-sprints";
import type { SprintCapacityMemberDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface CapacityFormProps {
  open: boolean;
  sprintId: string;
  projectId: string;
  currentCapacity: SprintCapacityMemberDto[];
  onClose: () => void;
}

interface CapacityEntry {
  memberId: string;
  memberName: string;
  points: number;
}

export function CapacityForm({
  open,
  sprintId,
  projectId,
  currentCapacity,
  onClose,
}: CapacityFormProps) {
  const [entries, setEntries] = useState<CapacityEntry[]>([]);
  const { mutate: updateCapacity, isPending } = useUpdateSprintCapacity(projectId);

  useEffect(() => {
    if (open) {
      setEntries(
        currentCapacity.map((m) => ({
          memberId: m.memberId,
          memberName: m.memberName,
          points: m.capacityPoints,
        })),
      );
    }
  }, [open, currentCapacity]);

  function handlePointsChange(memberId: string, value: string) {
    const parsed = parseInt(value, 10);
    setEntries((prev) =>
      prev.map((entry) =>
        entry.memberId === memberId
          ? { ...entry, points: isNaN(parsed) ? 0 : Math.max(0, parsed) }
          : entry,
      ),
    );
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    const capacity = entries.map((entry) => ({
      memberId: entry.memberId,
      points: entry.points,
    }));

    updateCapacity(
      { sprintId, data: { capacity } },
      {
        onSuccess: () => {
          toast.success("Capacity updated successfully.");
          onClose();
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to update capacity.");
        },
      },
    );
  }

  if (!open) return null;

  const totalCapacity = entries.reduce((sum, e) => sum + e.points, 0);

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 50,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(2px)",
      }}
      onClick={onClose}
    >
      <div
        data-testid="capacity-form"
        role="dialog"
        aria-modal="true"
        aria-labelledby="capacity-form-title"
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          boxShadow: "var(--tf-shadow)",
          width: "100%",
          maxWidth: 440,
          padding: 20,
        }}
      >
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            marginBottom: 16,
          }}
        >
          <h2
            id="capacity-form-title"
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 16,
              color: "var(--tf-text)",
            }}
          >
            Edit Capacity
          </h2>
          <button
            onClick={onClose}
            aria-label="Close dialog"
            style={{
              width: 26,
              height: 26,
              borderRadius: 5,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text3)",
              cursor: "pointer",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <X size={13} />
          </button>
        </div>

        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {entries.length === 0 ? (
            <p style={{ fontSize: 12, color: "var(--tf-text3)", textAlign: "center", padding: "12px 0" }}>
              No team members configured for this sprint.
            </p>
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {entries.map((entry) => (
                <div
                  key={entry.memberId}
                  data-testid={`capacity-member-${entry.memberId}`}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 10,
                  }}
                >
                  <span
                    style={{
                      fontSize: 13,
                      color: "var(--tf-text)",
                      fontWeight: 500,
                      flex: 1,
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {entry.memberName}
                  </span>
                  <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
                    <input
                      type="number"
                      min={0}
                      value={entry.points}
                      onChange={(e) => handlePointsChange(entry.memberId, e.target.value)}
                      aria-label={`Capacity for ${entry.memberName}`}
                      style={{
                        width: 70,
                        padding: "5px 8px",
                        borderRadius: 6,
                        border: "1px solid var(--tf-border)",
                        background: "var(--tf-bg3)",
                        color: "var(--tf-text)",
                        fontSize: 13,
                        outline: "none",
                        fontFamily: "var(--tf-font-mono)",
                        textAlign: "right",
                        transition: "border-color var(--tf-tr)",
                      }}
                      onFocus={(e) => {
                        (e.currentTarget as HTMLInputElement).style.borderColor = "var(--tf-accent)";
                      }}
                      onBlur={(e) => {
                        (e.currentTarget as HTMLInputElement).style.borderColor = "var(--tf-border)";
                      }}
                    />
                    <span
                      style={{
                        fontSize: 11,
                        color: "var(--tf-text3)",
                        fontFamily: "var(--tf-font-mono)",
                      }}
                    >
                      pts
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Total */}
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              paddingTop: 8,
              borderTop: "1px solid var(--tf-border)",
            }}
          >
            <span
              style={{
                fontSize: 12,
                color: "var(--tf-text2)",
                fontWeight: 600,
              }}
            >
              Total Capacity
            </span>
            <span
              style={{
                fontSize: 13,
                color: "var(--tf-text)",
                fontWeight: 600,
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              {totalCapacity} pts
            </span>
          </div>

          {/* Actions */}
          <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 4 }}>
            <button
              type="button"
              onClick={onClose}
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
              data-testid="capacity-save-btn"
              type="submit"
              disabled={isPending}
              style={{
                padding: "7px 14px",
                borderRadius: 6,
                border: "1px solid var(--tf-accent)",
                background: "var(--tf-accent)",
                color: "var(--primary-foreground)",
                fontSize: 12,
                fontWeight: 600,
                cursor: isPending ? "not-allowed" : "pointer",
                opacity: isPending ? 0.7 : 1,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {isPending ? "Saving..." : "Save Capacity"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
