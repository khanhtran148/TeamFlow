"use client";

import { useState } from "react";
import { UserCheck } from "lucide-react";
import { ApiError } from "@/lib/api/client";
import type { AdminOrganizationDto } from "@/lib/api/types";

interface TransferOwnershipDialogProps {
  org: AdminOrganizationDto;
  onConfirm: (orgId: string, newOwnerUserId: string) => Promise<void>;
  onClose: () => void;
}

export function TransferOwnershipDialog({
  org,
  onConfirm,
  onClose,
}: TransferOwnershipDialogProps) {
  const [newOwnerUserId, setNewOwnerUserId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!newOwnerUserId.trim()) {
      setError("New owner User ID is required.");
      return;
    }

    setLoading(true);
    try {
      await onConfirm(org.id, newOwnerUserId.trim());
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.problem.detail ?? err.problem.title);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("An unexpected error occurred.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="transfer-owner-title"
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
        onClick={onClose}
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
          maxWidth: 420,
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
              background: "var(--tf-blue-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <UserCheck size={15} color="var(--tf-blue)" />
          </div>
          <div>
            <h2
              id="transfer-owner-title"
              style={{
                fontSize: 15,
                fontWeight: 600,
                color: "var(--tf-text)",
                fontFamily: "var(--tf-font-head)",
                margin: 0,
              }}
            >
              Transfer Ownership
            </h2>
            <p
              style={{
                fontSize: 12,
                color: "var(--tf-text3)",
                margin: 0,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {org.name}
            </p>
          </div>
        </div>

        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text2)",
            marginBottom: 16,
            fontFamily: "var(--tf-font-body)",
            lineHeight: 1.5,
          }}
        >
          The current owner will be demoted to Admin. The new owner must already
          be a member of this organization.
        </p>

        <form
          onSubmit={handleSubmit}
          style={{ display: "flex", flexDirection: "column", gap: 12 }}
        >
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <label
              htmlFor="transfer-new-owner"
              style={{
                fontSize: 12,
                fontWeight: 500,
                color: "var(--tf-text2)",
                fontFamily: "var(--tf-font-body)",
              }}
            >
              New Owner User ID (UUID)
            </label>
            <input
              id="transfer-new-owner"
              type="text"
              value={newOwnerUserId}
              onChange={(e) => setNewOwnerUserId(e.target.value)}
              placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
              required
              autoFocus
              autoComplete="off"
              style={{
                padding: "9px 12px",
                borderRadius: 8,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text)",
                fontSize: 13,
                fontFamily: "var(--tf-font-mono)",
                outline: "none",
                transition: "border-color 0.15s",
              }}
              onFocus={(e) => {
                e.currentTarget.style.borderColor = "var(--tf-accent)";
              }}
              onBlur={(e) => {
                e.currentTarget.style.borderColor = "var(--tf-border)";
              }}
            />
          </div>

          {error && (
            <div
              role="alert"
              style={{
                padding: "8px 12px",
                borderRadius: 6,
                background: "rgba(248, 113, 113, 0.1)",
                border: "1px solid rgba(248, 113, 113, 0.3)",
                color: "var(--tf-red)",
                fontSize: 12,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {error}
            </div>
          )}

          <div style={{ display: "flex", gap: 8, marginTop: 4 }}>
            <button
              type="button"
              onClick={onClose}
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
              type="submit"
              disabled={loading}
              style={{
                flex: 1,
                padding: "8px 0",
                borderRadius: 6,
                border: "none",
                background: "var(--tf-blue)",
                color: "#0a0a0b",
                fontSize: 13,
                fontWeight: 600,
                fontFamily: "var(--tf-font-body)",
                cursor: loading ? "not-allowed" : "pointer",
                opacity: loading ? 0.7 : 1,
                minHeight: 36,
              }}
            >
              {loading ? "Transferring..." : "Transfer Ownership"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
