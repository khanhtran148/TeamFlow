"use client";

import { Loader2, AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { useDeleteWorkItem } from "@/lib/hooks/use-work-items";
import type { WorkItemDto } from "@/lib/api/types";

interface DeleteWorkItemDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  workItem: WorkItemDto;
  projectId: string;
  onDeleted: () => void;
}

export function DeleteWorkItemDialog({
  open,
  onOpenChange,
  workItem,
  projectId,
  onDeleted,
}: DeleteWorkItemDialogProps) {
  const deleteMutation = useDeleteWorkItem(projectId);

  async function handleDelete() {
    try {
      await deleteMutation.mutateAsync(workItem.id);
      toast.success(`"${workItem.title}" deleted`);
      onOpenChange(false);
      onDeleted();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete work item";
      toast.error(message);
    }
  }

  const hasChildren = workItem.childCount > 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
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
              display: "flex",
              alignItems: "center",
              gap: 8,
            }}
          >
            <AlertTriangle size={16} />
            Delete Work Item
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
          <p style={{ marginBottom: 10 }}>
            Are you sure you want to delete{" "}
            <strong style={{ color: "var(--tf-text)" }}>"{workItem.title}"</strong>?
          </p>

          {hasChildren && (
            <div
              style={{
                background: "var(--tf-red-dim)",
                border: "1px solid var(--tf-red)",
                borderRadius: 6,
                padding: "10px 14px",
                fontSize: 13,
                color: "var(--tf-red)",
                marginBottom: 10,
              }}
            >
              <strong>Warning:</strong> This item has{" "}
              <strong>{workItem.childCount}</strong> child item
              {workItem.childCount !== 1 ? "s" : ""}. All children will be
              soft-deleted as well (cascade delete).
            </div>
          )}

          <p style={{ fontSize: 13, color: "var(--tf-text3)" }}>
            This action performs a soft-delete. The item will be hidden from all
            views but can be recovered by an administrator.
          </p>
        </div>

        <DialogFooter style={{ marginTop: 16 }}>
          <Button
            type="button"
            variant="ghost"
            onClick={() => onOpenChange(false)}
            disabled={deleteMutation.isPending}
            style={{ color: "var(--tf-text2)" }}
          >
            Cancel
          </Button>
          <Button
            type="button"
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            style={{
              background: "var(--tf-red)",
              color: "var(--destructive-foreground)",
              fontWeight: 600,
            }}
          >
            {deleteMutation.isPending ? (
              <>
                <Loader2 size={13} className="animate-spin" />
                Deleting…
              </>
            ) : (
              "Delete"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
