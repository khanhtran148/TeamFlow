"use client";

import { ConfirmDialog } from "@/components/projects/confirm-dialog";

interface AddItemConfirmationProps {
  open: boolean;
  itemTitle: string;
  isPending: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function AddItemConfirmation({
  open,
  itemTitle,
  isPending,
  onConfirm,
  onCancel,
}: AddItemConfirmationProps) {
  return (
    <ConfirmDialog
      open={open}
      title="Add to Active Sprint"
      message={`The sprint is currently active and scope-locked. Adding "${itemTitle}" will change the sprint scope. This action requires Team Manager approval. Continue?`}
      confirmLabel="Add Item"
      isPending={isPending}
      onConfirm={onConfirm}
      onCancel={onCancel}
      data-testid="add-item-confirm-dialog"
      confirmTestId="add-item-confirm-btn"
      cancelTestId="add-item-cancel-btn"
    />
  );
}
