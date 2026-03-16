"use client";

import { useState } from "react";
import { toast } from "sonner";
import { ChevronDown, Loader2 } from "lucide-react";
import { useChangeStatus, useWorkItemBlockers } from "@/lib/hooks/use-work-items";
import { StatusBadge } from "@/components/shared/status-badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import type { WorkItemStatus } from "@/lib/api/types";

const ALL_STATUSES: WorkItemStatus[] = [
  "ToDo",
  "InProgress",
  "InReview",
  "NeedsClarification",
  "Done",
  "Rejected",
];

interface StatusSelectProps {
  workItemId: string;
  projectId: string;
  currentStatus: WorkItemStatus;
  isBlocked?: boolean;
  onStatusChanged?: (newStatus: WorkItemStatus) => void;
}

export function StatusSelect({
  workItemId,
  projectId,
  currentStatus,
  isBlocked = false,
  onStatusChanged,
}: StatusSelectProps) {
  const [open, setOpen] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<WorkItemStatus | null>(null);
  const [showBlockedDialog, setShowBlockedDialog] = useState(false);

  const changeStatusMutation = useChangeStatus(projectId);

  // Only fetch blockers when we need them (pending to InProgress)
  const { data: blockersData } = useWorkItemBlockers(workItemId, {
    enabled: showBlockedDialog,
  });

  async function applyStatus(status: WorkItemStatus) {
    try {
      await changeStatusMutation.mutateAsync({ id: workItemId, data: { status } });
      toast.success(`Status changed to ${statusLabel(status)}`);
      onStatusChanged?.(status);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to change status";
      toast.error(message);
    }
  }

  async function handleSelect(status: WorkItemStatus) {
    setOpen(false);
    if (status === currentStatus) return;

    if (status === "InProgress" && isBlocked) {
      // Only show blocker dialog if item is actually blocked
      setPendingStatus(status);
      setShowBlockedDialog(true);
      return;
    }

    await applyStatus(status);
  }

  async function handleConfirmBlocked() {
    setShowBlockedDialog(false);
    if (pendingStatus) {
      await applyStatus(pendingStatus);
      setPendingStatus(null);
    }
  }

  function handleCancelBlocked() {
    setShowBlockedDialog(false);
    setPendingStatus(null);
  }

  const hasBlockers = blockersData?.hasUnresolvedBlockers ?? false;

  return (
    <>
      {/* Dropdown trigger */}
      <div style={{ position: "relative", display: "inline-block" }}>
        <button
          onClick={() => setOpen((v) => !v)}
          style={{
            display: "flex",
            alignItems: "center",
            gap: 6,
            background: "none",
            border: "none",
            cursor: "pointer",
            padding: 0,
          }}
        >
          <StatusBadge status={currentStatus} size="md" />
          <ChevronDown size={12} style={{ color: "var(--tf-text3)" }} />
        </button>

        {open && (
          <>
            {/* Backdrop */}
            <div
              style={{ position: "fixed", inset: 0, zIndex: 40 }}
              onClick={() => setOpen(false)}
            />
            <div
              style={{
                position: "absolute",
                top: "calc(100% + 4px)",
                left: 0,
                zIndex: 50,
                background: "var(--tf-bg3)",
                border: "1px solid var(--tf-border)",
                borderRadius: 8,
                padding: "4px 0",
                minWidth: 180,
                boxShadow: "var(--tf-shadow)",
              }}
            >
              {ALL_STATUSES.map((s) => (
                <button
                  key={s}
                  onClick={() => handleSelect(s)}
                  disabled={changeStatusMutation.isPending}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    width: "100%",
                    padding: "7px 12px",
                    background: s === currentStatus ? "var(--tf-bg4)" : "none",
                    border: "none",
                    cursor: "pointer",
                    textAlign: "left",
                  }}
                >
                  <StatusBadge status={s} size="sm" />
                </button>
              ))}
            </div>
          </>
        )}
      </div>

      {/* Blocked confirm dialog */}
      <Dialog open={showBlockedDialog} onOpenChange={handleCancelBlocked}>
        <DialogContent
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            color: "var(--tf-text)",
            maxWidth: 460,
          }}
        >
          <DialogHeader>
            <DialogTitle
              style={{
                fontFamily: "var(--tf-font-head)",
                color: "var(--tf-red)",
                fontSize: 15,
              }}
            >
              Item Has Unresolved Blockers
            </DialogTitle>
          </DialogHeader>

          <div
            style={{
              fontSize: 13,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-body)",
              lineHeight: 1.6,
            }}
          >
            {hasBlockers ? (
              <>
                <p style={{ marginBottom: 10 }}>
                  This item is blocked by the following unresolved items. Moving it
                  to <strong style={{ color: "var(--tf-text)" }}>In Progress</strong>{" "}
                  will override the blockers.
                </p>
                <div
                  style={{
                    background: "var(--tf-bg3)",
                    border: "1px solid var(--tf-border)",
                    borderRadius: 6,
                    padding: "8px 12px",
                  }}
                >
                  {blockersData?.blockers.map((b) => (
                    <div
                      key={b.blockerId}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: 8,
                        padding: "4px 0",
                        fontSize: 13,
                        color: "var(--tf-text2)",
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
                      {b.title}
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <p>
                Move this item to{" "}
                <strong style={{ color: "var(--tf-text)" }}>In Progress</strong>?
              </p>
            )}
          </div>

          <DialogFooter style={{ marginTop: 16 }}>
            <Button
              type="button"
              variant="ghost"
              onClick={handleCancelBlocked}
              style={{ color: "var(--tf-text2)" }}
            >
              Cancel
            </Button>
            <Button
              type="button"
              onClick={handleConfirmBlocked}
              disabled={changeStatusMutation.isPending}
              style={{
                background: "var(--tf-red)",
                color: "var(--destructive-foreground)",
                fontWeight: 600,
              }}
            >
              {changeStatusMutation.isPending ? (
                <>
                  <Loader2 size={13} className="animate-spin" />
                  Changing…
                </>
              ) : (
                "Move Anyway"
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

function statusLabel(s: WorkItemStatus): string {
  const map: Record<WorkItemStatus, string> = {
    ToDo: "To Do",
    InProgress: "In Progress",
    InReview: "In Review",
    NeedsClarification: "Needs Clarification",
    Done: "Done",
    Rejected: "Rejected",
  };
  return map[s];
}
