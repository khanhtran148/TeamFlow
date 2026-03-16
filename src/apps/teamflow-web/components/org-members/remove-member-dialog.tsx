"use client";

import { AlertTriangle } from "lucide-react";
import type { OrganizationMemberDto } from "@/lib/api/types";

interface RemoveMemberDialogProps {
  open: boolean;
  member: OrganizationMemberDto;
  isPending: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function RemoveMemberDialog({
  open,
  member,
  isPending,
  onConfirm,
  onCancel,
}: RemoveMemberDialogProps) {
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
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="remove-member-dialog-title"
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
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: 8,
              background: "var(--tf-red-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            <AlertTriangle size={15} color="var(--tf-red)" />
          </div>
          <h3
            id="remove-member-dialog-title"
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 14,
              color: "var(--tf-text)",
            }}
          >
            Remove Member
          </h3>
        </div>

        <p style={{ fontSize: 13, color: "var(--tf-text3)", lineHeight: 1.5 }}>
          Are you sure you want to remove{" "}
          <strong style={{ color: "var(--tf-text)" }}>{member.userName}</strong>{" "}
          ({member.userEmail}) from this organization? They will lose access
          immediately.
        </p>

        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
          <button
            type="button"
            onClick={onCancel}
            disabled={isPending}
            style={{
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 13,
              fontWeight: 500,
              cursor: isPending ? "not-allowed" : "pointer",
              opacity: isPending ? 0.5 : 1,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={isPending}
            style={{
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-red)",
              background: "var(--tf-red-dim)",
              color: "var(--tf-red)",
              fontSize: 13,
              fontWeight: 600,
              cursor: isPending ? "not-allowed" : "pointer",
              opacity: isPending ? 0.7 : 1,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            {isPending ? "Removing..." : "Remove"}
          </button>
        </div>
      </div>
    </div>
  );
}
